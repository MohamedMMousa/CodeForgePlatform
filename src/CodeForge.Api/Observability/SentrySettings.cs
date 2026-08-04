namespace CodeForge.Api.Observability
{
    /// <summary>
    /// Dsn/Environment are read directly from configuration in Program.cs (Sentry has
    /// its own SDK-wide options object, wired up before the DI container exists); this
    /// class only needs to carry EnableTestEndpoint through DI to DiagnosticsController.
    /// </summary>
    public class SentrySettings
    {
        public const string SectionName = "Sentry";

        public string Dsn { get; set; } = string.Empty;
        public string? Environment { get; set; }

        /// <summary>Gates POST /diagnostics/sentry-test. Turn on temporarily post-deploy
        /// to confirm Sentry is receiving events, then off.</summary>
        public bool EnableTestEndpoint { get; set; }
    }
}
