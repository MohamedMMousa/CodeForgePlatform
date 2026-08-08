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
        /// Empty by default — opt in only for a proxy that genuinely sets such a header.
        /// It briefly defaulted to "X-Real-IP" on the belief that the Vercel → Render
        /// chain reported the real client there and omitted it from X-Forwarded-For.
        /// Measuring the live deployment through GET /diagnostics/client-ip disproved
        /// both halves: X-Forwarded-For carries the real client as its leftmost entry,
        /// and no usable X-Real-IP arrives at all (the resolver fell through to the
        /// X-Forwarded-For branch on every sampled request). See TrustedProxyHopCount.
        ///
        /// Caveat if you do enable it: unlike the right-anchored X-Forwarded-For read, a
        /// single-value header carries no evidence of WHO set it. It is only as
        /// trustworthy as the guarantee that every path into this API passes through a
        /// proxy that OVERWRITES it. A caller who can reach the origin directly and set
        /// the header themselves would control their own rate-limit partition key.
        /// </summary>
        public string ClientIpHeader { get; set; } = string.Empty;

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
        /// That measurement has been made, and production runs at 3. The live chain is
        /// four entries — Cloudflare fronts Render, in front of Vercel:
        ///   &lt;real client&gt;, &lt;vercel egress 3.x&gt;, &lt;cloudflare 172.x/162.x&gt;, &lt;render 10.x&gt;
        /// Only the first is stable; the other three rotate per request. At 0 the
        /// resolver therefore partitioned on a rotating private 10.x address, giving
        /// every request its own bucket and silently disabling rate limiting entirely —
        /// 12 consecutive public submissions passed against a 5/minute policy before
        /// this was caught. 1 + 3 = 4 from the right lands on the real client, and stays
        /// forgery-resistant: a prepended entry lengthens the chain without moving the
        /// counted-from-the-right position.
        ///
        /// A resolved key in a private range (10.x, 172.16-31.x, 192.168.x) or 127.0.0.1
        /// is the signature of this being wrong — re-measure if the topology changes.
        /// </summary>
        public int TrustedProxyHopCount { get; set; }

        /// <summary>
        /// Gates GET /diagnostics/client-ip. Turn on temporarily post-deploy to measure
        /// the real proxy chain and derive the correct TrustedProxyHopCount, then off.
        /// </summary>
        public bool EnableDiagnostics { get; set; }
    }
}
