namespace CodeForge.Api.RateLimiting
{
    /// <summary>
    /// Named rate-limit policies applied to sensitive/public endpoints via
    /// [EnableRateLimiting(...)]. A generous global per-IP limiter backs everything else.
    /// </summary>
    public static class RateLimitPolicies
    {
        /// <summary>Login, refresh, forgot/reset password — brute-force sensitive.</summary>
        public const string Auth = "auth";

        /// <summary>Anonymous public submissions (enrollment requests, lead form).</summary>
        public const string PublicSubmit = "public-submit";
    }
}
