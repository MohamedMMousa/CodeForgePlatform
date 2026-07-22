using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Infrastructure.Assessments;
using CodeForge.Infrastructure.Authentication;
using CodeForge.Infrastructure.Data;
using CodeForge.Infrastructure.Email;
using CodeForge.Infrastructure.EnrollmentRequests;

namespace CodeForge.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // PostgreSQL Database Configuration using EF Core & Npgsql
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            services.AddDbContext<CodeForgeDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(CodeForgeDbContext).Assembly.FullName)));

            // Register database interface for Clean Architecture
            services.AddScoped<ICodeForgeDbContext>(provider => 
                provider.GetRequiredService<CodeForgeDbContext>());

            // JWT Settings Mapping
            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));

            // Admin bootstrap credentials (used by DatabaseSeeder at startup)
            services.Configure<AdminSeedSettings>(
                configuration.GetSection(AdminSeedSettings.SectionName));

            // Email Settings + sender selection (real SMTP when enabled, else dev logger)
            var emailSection = configuration.GetSection(EmailSettings.SectionName);
            services.Configure<EmailSettings>(emailSection);
            var emailSettings = emailSection.Get<EmailSettings>() ?? new EmailSettings();
            if (emailSettings.Enabled && !string.IsNullOrWhiteSpace(emailSettings.Host))
            {
                services.AddScoped<IEmailSender, SmtpEmailSender>();
            }
            else
            {
                services.AddScoped<IEmailSender, LoggingEmailSender>();
            }

            // Authentication Services Registration
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
            services.AddScoped<IEnrollmentNotificationService, LoggingEnrollmentNotificationService>();

            // Auto-grader: Piston (emkc.org), a free hosted sandboxed code-execution
            // API — no Docker required locally. Swap for a self-hosted engine once
            // hosting is decided (Phase 5).
            services.AddHttpClient<ICodeExecutionService, PistonCodeExecutionService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            return services;
        }
    }
}
