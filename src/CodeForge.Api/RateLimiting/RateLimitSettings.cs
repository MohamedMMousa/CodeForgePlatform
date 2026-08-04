namespace CodeForge.Api.RateLimiting
{
    /// <summary>
    /// Tunable permit/window pairs for the global limiter and the two named policies —
    /// see Program.cs's AddRateLimiter call. Defaults match the values that were
    /// previously hardcoded, so an unconfigured environment behaves exactly as before.
    /// </summary>
    public class RateLimitSettings
    {
        public const string SectionName = "RateLimiting";

        public RateLimitWindow Global { get; set; } = new() { PermitLimit = 100, WindowSeconds = 60 };
        public RateLimitWindow Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
        public RateLimitWindow PublicSubmit { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };
    }

    public class RateLimitWindow
    {
        public int PermitLimit { get; set; }
        public int WindowSeconds { get; set; }
    }
}
