using System.Net;

namespace CodeForge.Api.RateLimiting
{
    /// <summary>
    /// Resolves the rate-limit partition key for a request. Exists because behind
    /// Vercel → Render, HttpContext.Connection.RemoteIpAddress is always Vercel's shared
    /// egress IP — trusting it as-is would collapse every user on the platform into one
    /// bucket. See ProxySettings for the trust model and why the header is read from the
    /// RIGHT end (the entry contributed by the hop we control) rather than the left (the
    /// entry the caller itself claims, which anyone can forge).
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

            if (!context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValues))
            {
                return socketPeer ?? "unknown";
            }

            return Resolve(headerValues, settings.TrustedProxyHopCount, socketPeer);
        }

        private static string Resolve(
            IEnumerable<string?> forwardedForHeaderValues, int trustedProxyHopCount, string? socketPeer)
        {
            // A single "X-Forwarded-For" header can itself be a comma-joined list, and
            // an HTTP request can carry multiple header lines with the same name —
            // flatten both before indexing from the right.
            var entries = forwardedForHeaderValues
                .Where(value => !string.IsNullOrEmpty(value))
                .SelectMany(value => value!.Split(','))
                .Select(ParseEntry)
                .Where(ip => ip is not null)
                .Select(ip => ip!.ToString())
                .ToList();

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
