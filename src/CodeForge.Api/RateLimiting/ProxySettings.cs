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
        /// Name of the single-value header the immediate proxy sets to the real client
        /// address. Tried BEFORE the X-Forwarded-For positional logic below, and only
        /// when TrustForwardedFor is on — so local dev and CI are unaffected by it.
        ///
        /// Defaults to "X-Real-IP" because that is what the live Vercel → Render chain
        /// was measured to populate: Vercel reports the real client there and does not
        /// append it to X-Forwarded-For, which leaves TrustedProxyHopCount with no
        /// client entry to count to no matter what it's set to. Defaulting it here
        /// rather than requiring a new Render env var means an existing deployment
        /// picks up the fix on redeploy.
        ///
        /// Caveat worth knowing before relying on this: unlike the right-anchored
        /// X-Forwarded-For read, a single-value header carries no evidence of WHO set
        /// it. It is only as trustworthy as the guarantee that every path into this API
        /// passes through a proxy that OVERWRITES it. If the API is also reachable
        /// directly (e.g. its raw Render URL) by a caller who sets X-Real-IP themselves,
        /// that caller controls their own rate-limit partition key. Set this to empty to
        /// disable the header entirely and fall back to X-Forwarded-For only.
        /// </summary>
        public string ClientIpHeader { get; set; } = "X-Real-IP";

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
        ///
        /// That measurement has since been made, and on the live Vercel → Render chain
        /// this knob turned out not to apply at all: the real client IP arrives in
        /// X-Real-IP and is never appended to X-Forwarded-For, so no hop count reaches
        /// it. ClientIpHeader above is what's load-bearing in production now; this
        /// remains the fallback for a proxy that only sets X-Forwarded-For.
        /// </summary>
        public int TrustedProxyHopCount { get; set; }

        /// <summary>
        /// Gates GET /diagnostics/client-ip. Turn on temporarily post-deploy to measure
        /// the real proxy chain and derive the correct TrustedProxyHopCount, then off.
        /// </summary>
        public bool EnableDiagnostics { get; set; }
    }
}
