namespace CodeForge.Api.RateLimiting
{
    /// <summary>
    /// Controls how much the API trusts a reverse proxy's X-Forwarded-For header when
    /// resolving the "real" client IP for rate-limit partitioning. See ClientIpResolver.
    /// </summary>
    public class ProxySettings
    {
        public const string SectionName = "Proxy";

        /// <summary>
        /// False (default — matches local dev, CI, and docker-compose/Caddy) makes
        /// ClientIpResolver ignore X-Forwarded-For entirely and use the socket peer.
        /// Set true in production behind Vercel → Render, where the socket peer is
        /// always Vercel's shared egress IP and would otherwise collapse every user
        /// into one rate-limit bucket.
        /// </summary>
        public bool TrustForwardedFor { get; set; }

        /// <summary>
        /// How many entries to discard from the RIGHT end of X-Forwarded-For — i.e. how
        /// many proxy hops between the real client and this API are trusted to have each
        /// appended their own entry. 0 (the conservative default) reads the rightmost
        /// entry itself, which is correct if there is exactly one such hop. Vercel
        /// overwrites X-Forwarded-For rather than forwarding a client-supplied one, so
        /// the chain reaching this API is expected to be "[client, vercel-egress]" — but
        /// the exact count must be MEASURED against the real deployment (see
        /// GET /diagnostics/client-ip, gated by EnableDiagnostics below), not assumed,
        /// since it also depends on Render's edge and on whether the Next.js rewrite
        /// double-proxies the request.
        /// </summary>
        public int TrustedProxyHopCount { get; set; }

        /// <summary>
        /// Gates GET /diagnostics/client-ip. Turn on temporarily post-deploy to measure
        /// the real proxy chain and derive the correct TrustedProxyHopCount, then off.
        /// </summary>
        public bool EnableDiagnostics { get; set; }
    }
}
