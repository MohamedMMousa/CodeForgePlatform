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
using CodeForge.Api.Filters;
using CodeForge.Api.Middleware;
using CodeForge.Api.RateLimiting;
using CodeForge.Api.Swagger;
using CodeForge.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// PasswordChangeRequiredFilter is global (fail-closed): it blocks every authenticated
// endpoint for a user whose token says MustChangePassword, unless the endpoint opts out
// via [AllowAnonymous] or [AllowPendingPasswordChange]. See ARCHITECTURE.md §3.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<PasswordChangeRequiredFilter>();
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

// Rate limiting: a generous global per-IP limiter, plus stricter named policies
// for brute-force-sensitive auth and anonymous public submissions.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy(RateLimitPolicies.Auth, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy(RateLimitPolicies.PublicSubmit, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
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
});

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

// Behind the compose Caddy reverse proxy, requests arrive from the proxy's container
// IP, not the real client — this restores the real client IP/scheme from
// X-Forwarded-For/-Proto before HTTPS redirection or the rate limiter (both of which
// key off RemoteIpAddress/Scheme) see the request. KnownNetworks/KnownProxies are
// cleared because the proxy's address is a private compose-network IP, not loopback —
// safe here because the api service isn't reachable except through Caddy.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
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
