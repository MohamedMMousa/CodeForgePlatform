using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CodeForge.Application;
using CodeForge.Application.Common.Constants;
using CodeForge.Infrastructure;
using CodeForge.Application.Common.Models;
using CodeForge.Api.Authentication;
using CodeForge.Api.Filters;
using CodeForge.Api.Middleware;
using CodeForge.Api.Observability;
using CodeForge.Api.RateLimiting;
using CodeForge.Api.Serialization;
using CodeForge.Api.Swagger;
using CodeForge.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Error monitoring: only initialized when a DSN is configured, so local dev and CI
// (which set neither) never talk to Sentry at all. TracesSampleRate=0 — the free plan's
// quota is spent on errors, not performance traces. SendDefaultPii=false (also the SDK
// default, restated for clarity) keeps cookies out entirely; SetBeforeSend strips the
// specific headers that could still carry a session identifier or CSRF token even
// without full PII capture. See DiagnosticsController for how a test event gets
// emitted, and ExceptionHandlingMiddleware for why capture rides the ILogger
// integration rather than Sentry's own exception middleware: that middleware catches
// and never rethrows, so nothing ever reaches Sentry's exception handler directly.
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Configuration["Sentry:Environment"] ?? builder.Environment.EnvironmentName;
        options.SendDefaultPii = false;
        options.TracesSampleRate = 0;
        options.SetBeforeSend((sentryEvent, _) =>
        {
            if (sentryEvent.Request is not null)
            {
                sentryEvent.Request.Cookies = null;
                foreach (var headerName in new[] { "Cookie", "Authorization", "X-CSRF-Token" })
                {
                    sentryEvent.Request.Headers.Remove(headerName);
                }
            }
            return sentryEvent;
        });
    });
}
builder.Services.Configure<SentrySettings>(builder.Configuration.GetSection(SentrySettings.SectionName));

// Add services to the container.
// PasswordChangeRequiredFilter is global (fail-closed): it blocks every authenticated
// endpoint for a user whose token says MustChangePassword, unless the endpoint opts out
// via [AllowAnonymous] or [AllowPendingPasswordChange]. See ARCHITECTURE.md §3.
// CsrfProtectionFilter is also global: it only acts on unsafe requests that carry an
// auth cookie, so it's a no-op for anonymous public endpoints. See ARCHITECTURE.md §3.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<PasswordChangeRequiredFilter>();
    options.Filters.Add<CsrfProtectionFilter>();
}).AddJsonOptions(options =>
{
    // Timestamps land in `timestamptz` columns, which Npgsql only accepts as UTC.
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
});

// Localization scaffolding: resolve culture per request (Arabic/English) from the
// Accept-Language header or a ?culture= query string. Resource files can be added
// incrementally; the pipeline is ready for them now.
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("ar")
    };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
});

// Configure Swagger/OpenAPI with JWT Bearer Authentication Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeForge Academy API",
        Version = "v1"
    });

    // Reflects C# nullable-reference annotations into the schema's `nullable` flags,
    // and RequireNonNullablePropertiesSchemaFilter promotes non-nullable properties to
    // `required` — together these make the generated OpenAPI doc exactly as strict as
    // the DTOs themselves, which frontend/lib/api-schema.d.ts is generated from
    // (see scripts/generate-api-types.mjs).
    options.SupportNonNullableReferenceTypes();
    options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register Application & Infrastructure Services (EF Core PostgreSQL, DI, etc.)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// CORS: allow the front-end origin(s) to call the API from the browser.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Proxy trust + rate-limit windows: bound once here (not IOptionsMonitor — same
// bind-once-at-startup pattern as jwtSettings/corsOrigins below) since both are read
// from the partition-key lambdas below and from DiagnosticsController.
var proxySettings = builder.Configuration.GetSection(ProxySettings.SectionName).Get<ProxySettings>()
    ?? new ProxySettings();
builder.Services.Configure<ProxySettings>(builder.Configuration.GetSection(ProxySettings.SectionName));

var rateLimitSettings = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>()
    ?? new RateLimitSettings();

// Rate limiting: a generous global per-IP limiter, plus stricter named policies
// for brute-force-sensitive auth and anonymous public submissions. Partition key is
// ClientIpResolver.Resolve, not the raw socket address — see ProxySettings for why.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientIpResolver.Resolve(context, proxySettings),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.Global.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.Global.WindowSeconds)
            }));

    options.AddPolicy(RateLimitPolicies.Auth, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientIpResolver.Resolve(context, proxySettings),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.Auth.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.Auth.WindowSeconds)
            }));

    options.AddPolicy(RateLimitPolicies.PublicSubmit, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientIpResolver.Resolve(context, proxySettings),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.PublicSubmit.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.PublicSubmit.WindowSeconds)
            }));

    // A rejection was previously silent: no Retry-After, no log line, so a spike of
    // 429s was invisible until someone reported it. Both come from the rejecting
    // limiter's own lease metadata / the same partition-key resolution used above.
    options.OnRejected = (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var partitionKey = ClientIpResolver.Resolve(context.HttpContext, proxySettings);
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("CodeForge.Api.RateLimiting");
        logger.LogWarning(
            "Rate limit exceeded for {PartitionKey} on {Path}.",
            partitionKey, context.HttpContext.Request.Path);

        return ValueTask.CompletedTask;
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("InstructorOnly", policy => policy.RequireRole(Roles.Instructor));
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.Secret))
{
    throw new InvalidOperationException(
        "JWT settings are not configured. Set 'JwtSettings:Secret' via user-secrets " +
        "(dev) or environment variables (production). It must not live in appsettings.json.");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. Set it via user-secrets " +
        "(dev) or environment variables (production).");
}

// Readiness (not liveness) only: DB connectivity is what "ready" means, and only
// /health/ready is wired to it. See the two app.UseHealthChecks(...) calls below.
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: new[] { "ready" });

var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        // The cookie is the primary source now; leaving context.Token unset when the
        // cookie is absent lets the handler fall back to the Authorization header
        // itself, which keeps Swagger and any direct/dev client working unchanged.
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue(AuthCookieWriter.AccessTokenCookieName, out var accessToken) &&
                !string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSingleton<AuthCookieWriter>();

var app = builder.Build();

// Off by default everywhere — dev/CI apply migrations via the `dotnet ef` CLI so a
// developer sees exactly what's about to run. docker-compose sets this true so a
// fresh Postgres container gets schema without a manual step; a real deployment
// should keep it false and run migrations as a separate release step.
if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<CodeForgeDbContext>();
    await db.Database.MigrateAsync();
}

// Bootstrap the initial super-admin (idempotent; no-op unless AdminSeed is configured).
await DatabaseSeeder.SeedAsync(app.Services);

// Silent-email-in-prod guard: unconfigured EmailSettings binds LoggingEmailSender in
// Production too (see DependencyInjection.cs), so password resets and notification
// emails would otherwise report success and deliver nothing with zero signal. This is
// a loud Critical log rather than a fail-fast throw (unlike the JWT-secret/connection-
// string guards above) because the deployed Render API has no mail provider yet during
// this pre-pilot period — throwing here would take down an otherwise-healthy API.
// LogCritical (not Warning) is deliberate: Sentry's ILogger integration captures at
// Error+, so once Sentry__Dsn is set this also raises a Sentry event per deploy.
// Once email is activated, turn this into a fail-fast throw like the guards above.
if (app.Environment.IsProduction())
{
    var emailSettings = app.Services.GetRequiredService<IOptions<EmailSettings>>().Value;
    if (!emailSettings.Enabled || string.IsNullOrWhiteSpace(emailSettings.Host))
    {
        app.Logger.LogCritical(
            "EMAIL IS NOT CONFIGURED IN PRODUCTION. IEmailSender is bound to " +
            "LoggingEmailSender, which delivers nothing: password resets and all " +
            "notification emails will silently fail. Set EmailSettings__Enabled=true " +
            "and EmailSettings__Host to activate delivery.");
    }
}

// Translate handler/validation exceptions into a consistent ProblemDetails envelope.
// Registered first so it wraps the entire downstream pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Unauthenticated liveness/readiness probes. Placed before HTTPS redirection (so a
// plain-HTTP probe isn't 307'd), before the rate limiter (so a frequent probe can't
// exhaust the per-IP bucket), and before auth/MVC entirely — so no [AllowAnonymous] or
// [AllowPendingPasswordChange] is needed and PasswordChangeRequiredFilter never runs.
// /health = liveness (process is up; no DB check) — this is what the host's restart
// probe should hit, since restarting the instance can't fix a DB outage.
// /health/ready = readiness (DB reachable) — for compose depends_on / humans, never
// wired to anything that would restart the process on a transient DB blip.
app.UseHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.UseHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

// Proto only — restores the real scheme (https) from X-Forwarded-Proto before HTTPS
// redirection sees the request, behind both the compose Caddy proxy and, in
// production, Render's edge. Deliberately NOT XForwardedFor: this middleware's default
// ForwardLimit of 1 would overwrite Connection.RemoteIpAddress with a single header
// entry and then strip it, leaving nothing for ClientIpResolver to read and no reliable
// untrusted fallback either. Instead RemoteIpAddress is left alone as the true socket
// peer — the fail-closed fallback ClientIpResolver uses when Proxy:TrustForwardedFor is
// off, or when the header doesn't have the hop count Proxy:TrustedProxyHopCount claims
// — and ClientIpResolver reads the untouched raw header itself. See ProxySettings.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
};
// The middleware only trusts X-Forwarded-Proto from a RemoteIpAddress it recognizes as
// a proxy; default KnownNetworks/KnownProxies wouldn't match Caddy's private
// compose-network IP or Render's edge IP, so proto forwarding would silently do
// nothing. Cleared so it's trusted from whichever address is connecting — safe because
// the container is never reachable except through that platform edge (Caddy in
// compose, Render's edge in production), so the immediate TCP peer is always the
// platform itself, never an arbitrary internet client.
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeForge Academy API v1");
    });
}

app.UseRequestLocalization(
    app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.UseHttpsRedirection();

// No app.UseStaticFiles() — uploaded files (payment proofs, course materials) are
// private and served exclusively through authenticated endpoints backed by
// IFileStorageService, which stores them outside wwwroot entirely. See
// docs/ARCHITECTURE.md §1 and §3.

app.UseCors();

app.UseRateLimiter();

// Enable Authentication and Authorization middlewares
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
