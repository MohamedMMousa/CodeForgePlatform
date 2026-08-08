using System.Net;

namespace CodeForge.Api.RateLimiting
{
    /// <summary>
    /// Resolves the rate-limit partition key for a request. Exists because behind
    /// Vercel → Render, HttpContext.Connection.RemoteIpAddress is always Vercel's shared
    /// egress IP — trusting it as-is would collapse every user on the platform into one
    /// bucket. See ProxySettings for the trust model and why X-Forwarded-For is read from
    /// the RIGHT end (the entry contributed by the hop we control) rather than the left
    /// (the entry the caller itself claims, which anyone can forge).
    ///
    /// Two sources, in order: ProxySettings.ClientIpHeader (off by default — this
    /// deployment gets no usable single-value header, measured, see ProxySettings)
    /// first, then the X-Forwarded-For positional read, which is what production
    /// actually runs on at TrustedProxyHopCount=3. Both are gated behind
    /// TrustForwardedFor, so local dev and CI still partition on the socket peer.
    ///
    /// Not registered in DI — this is a pure function over inputs already on
    /// HttpContext, called both from the rate limiter's partition-key lambdas in
    /// Program.cs and from DiagnosticsController, and is exhaustively unit-tested
    /// against a DefaultHttpContext (see
    /// tests/CodeForge.UnitTests/RateLimiting/ClientIpResolverTests.cs), the same
    /// pattern CsrfProtectionFilterTests already uses for this kind of pipeline logic.
    /// </summary>
    public static class ClientIpResolver
    {
        public static string Resolve(HttpContext context, ProxySettings settings)
        {
            var socketPeer = context.Connection.RemoteIpAddress?.ToString();

            if (!settings.TrustForwardedFor)
            {
                return socketPeer ?? "unknown";
            }

            // Opt-in source, tried before X-Forwarded-For: a single-value header the
            // immediate proxy sets to the real client address. Off by default and NOT
            // used by this deployment — measurement showed no usable X-Real-IP arrives
            // here (see ProxySettings.ClientIpHeader). Kept because a named header has
            // no position to get wrong, which is genuinely better where one exists.
            if (!string.IsNullOrWhiteSpace(settings.ClientIpHeader)
                && context.Request.Headers.TryGetValue(settings.ClientIpHeader, out var clientIpValues))
            {
                var clientIpEntries = ParseEntries(clientIpValues);

                // Defined as a single value, so normally there's exactly one entry. If
                // something upstream appended rather than overwrote, take the rightmost
                // — the same right-anchored reasoning as X-Forwarded-For below: the
                // nearest hop is the only one whose contribution we can attribute.
                if (clientIpEntries.Count > 0)
                {
                    return clientIpEntries[^1];
                }
            }

            // Fallback for deployments where the proxy only sets X-Forwarded-For, and
            // for local dev/docker-compose reached with TrustForwardedFor on.
            if (!context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValues))
            {
                return socketPeer ?? "unknown";
            }

            return ResolveFromForwardedFor(headerValues, settings.TrustedProxyHopCount, socketPeer);
        }

        private static string ResolveFromForwardedFor(
            IEnumerable<string?> forwardedForHeaderValues, int trustedProxyHopCount, string? socketPeer)
        {
            var entries = ParseEntries(forwardedForHeaderValues);

            var indexFromRight = 1 + trustedProxyHopCount;

            // Header missing, empty, unparseable, or shorter than the hop count we
            // claim to trust: fail closed to the untrusted-but-real socket peer rather
            // than guessing at a position that isn't there.
            if (entries.Count < indexFromRight)
            {
                return socketPeer ?? "unknown";
            }

            return entries[^indexFromRight];
        }

        /// <summary>
        /// A single header line can itself be a comma-joined list, and an HTTP request
        /// can carry multiple lines with the same name — flatten both, drop anything
        /// that isn't a parseable IP, and preserve left-to-right order.
        /// </summary>
        private static List<string> ParseEntries(IEnumerable<string?> headerValues)
        {
            return headerValues
                .Where(value => !string.IsNullOrEmpty(value))
                .SelectMany(value => value!.Split(','))
                .Select(ParseEntry)
                .Where(ip => ip is not null)
                .Select(ip => ip!.ToString())
                .ToList();
        }

        private static IPAddress? ParseEntry(string rawEntry)
        {
            var candidate = rawEntry.Trim();
            if (candidate.Length == 0)
            {
                return null;
            }

            // IPv6 entries may be bracketed with a port, e.g. "[::1]:12345".
            if (candidate[0] == '[')
            {
                var closingBracket = candidate.IndexOf(']');
                if (closingBracket <= 0)
                {
                    return null;
                }
                candidate = candidate[1..closingBracket];
            }
            else
            {
                // A bare IPv4 address never contains a colon, so a single colon here
                // unambiguously separates "ip:port" (bare IPv6 has more than one).
                var colonCount = candidate.Count(c => c == ':');
                if (colonCount == 1)
                {
                    candidate = candidate[..candidate.IndexOf(':')];
                }
            }

            return IPAddress.TryParse(candidate, out var ip) ? ip : null;
        }
    }
}
