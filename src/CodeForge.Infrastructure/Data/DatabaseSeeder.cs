using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeForge.Infrastructure.Data
{
    /// <summary>
    /// Idempotent startup seeding. Currently bootstraps the first super-admin so the
    /// platform is usable without manual DB edits. Runs only when admin credentials
    /// are configured, and never overwrites an existing account.
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var logger = provider.GetRequiredService<ILogger<CodeForgeDbContext>>();
            var settings = provider.GetRequiredService<IOptions<AdminSeedSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
            {
                logger.LogInformation(
                    "Admin seed skipped: no AdminSeed:Email/Password configured.");
                return;
            }

            var context = provider.GetRequiredService<CodeForgeDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

            var normalizedEmail = settings.Email.Trim().ToLower();
            var exists = await context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

            if (exists)
            {
                logger.LogInformation("Admin seed skipped: an account already exists for the configured email.");
                return;
            }

            context.Users.Add(new User
            {
                Email = settings.Email.Trim(),
                FullName = settings.FullName,
                Role = Roles.Admin,
                PasswordHash = passwordHasher.HashPassword(settings.Password),
                IsActive = true,
                MustChangePassword = true
            });

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded initial super-admin account for {Email}.", settings.Email);
        }
    }
}
