namespace CodeForge.Application.Common.Models
{
    /// <summary>
    /// Bootstrap credentials for the first super-admin. Supplied via user-secrets (dev)
    /// or environment variables (production) — never committed. Seeding is skipped unless
    /// both Email and Password are provided, so no account with a known default is created.
    /// </summary>
    public class AdminSeedSettings
    {
        public const string SectionName = "AdminSeed";

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = "Platform Administrator";
    }
}
