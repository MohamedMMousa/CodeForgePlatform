using System.Net;
using CodeForge.Api.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CodeForge.UnitTests.RateLimiting
{
    public class ClientIpResolverTests
    {
        private static HttpContext CreateContext(
            string? socketPeer, string? forwardedFor = null, string? realIp = null)
        {
            var context = new DefaultHttpContext();
            if (socketPeer is not null)
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(socketPeer);
            }
            if (forwardedFor is not null)
            {
                context.Request.Headers.Append("X-Forwarded-For", forwardedFor);
            }
            if (realIp is not null)
            {
                context.Request.Headers.Append("X-Real-IP", realIp);
            }
            return context;
        }

        [Fact]
        public void Resolve_WhenNotTrustingForwardedFor_AlwaysUsesSocketPeer_EvenWithHeaderPresent()
        {
            var context = CreateContext("203.0.113.9", forwardedFor: "9.9.9.9, 8.8.8.8");
            var settings = new ProxySettings { TrustForwardedFor = false, TrustedProxyHopCount = 1 };

            ClientIpResolver.Resolve(context, settings).Should().Be("203.0.113.9");
        }

        [Fact]
        public void Resolve_WhenTrustingForwardedFor_ButHeaderAbsent_FallsBackToSocketPeer()
        {
            var context = CreateContext("203.0.113.9");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            ClientIpResolver.Resolve(context, settings).Should().Be("203.0.113.9");
        }

        [Fact]
        public void Resolve_WithNoSocketPeerAndNoHeader_ReturnsUnknown()
        {
            var context = CreateContext(socketPeer: null);
            var settings = new ProxySettings { TrustForwardedFor = false };

            ClientIpResolver.Resolve(context, settings).Should().Be("unknown");
        }

        [Fact]
        public void Resolve_WithHopCountZero_ReadsTheRightmostEntry()
        {
            // hopCount 0 means "trust nothing beyond the immediate upstream" — the
            // entry the edge closest to us appended, i.e. the rightmost one.
            var context = CreateContext("10.0.0.1", forwardedFor: "198.51.100.5, 203.0.113.77");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            ClientIpResolver.Resolve(context, settings).Should().Be("203.0.113.77");
        }

        [Fact]
        public void Resolve_WithHopCountOne_SkipsPastTheNearestHop_ToTheEntryBeforeIt()
        {
            // Generic two-hop shape. NOT the production one — that was assumed to be
            // [real-client, vercel-egress] and measurement later showed four entries;
            // see Resolve_ForTheMeasuredProductionChain_* below for the real thing.
            var context = CreateContext("10.0.0.1", forwardedFor: "198.51.100.5, 203.0.113.77");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 1 };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }

        [Fact]
        public void Resolve_IgnoresEntriesPrependedToTheLeftOfTheTrustedPosition()
        {
            // Simulates a caller trying to spoof by stuffing extra entries in front —
            // the trusted position is still counted from the right, so a prepended
            // forged entry doesn't shift what gets selected.
            var context = CreateContext("10.0.0.1", forwardedFor: "forged-looking-but-valid-1.1.1.1, 198.51.100.5, 203.0.113.77");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 1 };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }

        [Fact]
        public void Resolve_WhenHopCountExceedsAvailableEntries_FailsClosedToSocketPeer()
        {
            var context = CreateContext("10.0.0.1", forwardedFor: "198.51.100.5");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 5 };

            ClientIpResolver.Resolve(context, settings).Should().Be("10.0.0.1");
        }

        [Fact]
        public void Resolve_StripsPortsFromIPv4Entries()
        {
            var context = CreateContext("10.0.0.1", forwardedFor: "203.0.113.77:54321");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            ClientIpResolver.Resolve(context, settings).Should().Be("203.0.113.77");
        }

        [Fact]
        public void Resolve_StripsBracketsAndPortsFromIPv6Entries()
        {
            var context = CreateContext("10.0.0.1", forwardedFor: "[2001:db8::1]:8080");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            ClientIpResolver.Resolve(context, settings).Should().Be("2001:db8::1");
        }

        [Fact]
        public void Resolve_HandlesBareIPv6EntriesWithoutBrackets()
        {
            var context = CreateContext("10.0.0.1", forwardedFor: "2001:db8::1");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            ClientIpResolver.Resolve(context, settings).Should().Be("2001:db8::1");
        }

        [Fact]
        public void Resolve_FiltersUnparseableEntries_ThenAppliesHopCountToWhatRemains()
        {
            var context = CreateContext("10.0.0.1", forwardedFor: "not-an-ip, 198.51.100.5");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }

        [Fact]
        public void Resolve_WhenAllEntriesUnparseable_FailsClosedToSocketPeer()
        {
            var context = CreateContext("10.0.0.1", forwardedFor: "not-an-ip, also-not-an-ip");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            ClientIpResolver.Resolve(context, settings).Should().Be("10.0.0.1");
        }

        [Fact]
        public void Resolve_TreatsMultipleHeaderLinesTheSameAsOneCommaJoinedLine()
        {
            var context = CreateContext("10.0.0.1");
            context.Request.Headers.Append("X-Forwarded-For", "198.51.100.5");
            context.Request.Headers.Append("X-Forwarded-For", "203.0.113.77");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            ClientIpResolver.Resolve(context, settings).Should().Be("203.0.113.77");
        }

        [Fact]
        public void Resolve_DistinctRealClients_MapToDistinctPartitionKeys()
        {
            // The actual bug this exists to fix: two different real users, proxied
            // through the same fixed Vercel egress IP, must not collapse into one key.
            var settingsForVercel = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 1 };

            var userA = CreateContext("10.0.0.1", forwardedFor: "198.51.100.5, 203.0.113.77");
            var userB = CreateContext("10.0.0.1", forwardedFor: "198.51.100.9, 203.0.113.77");

            ClientIpResolver.Resolve(userA, settingsForVercel)
                .Should().NotBe(ClientIpResolver.Resolve(userB, settingsForVercel));
        }

        // --- The real production chain, as measured through GET /diagnostics/client-ip.

        /// <summary>
        /// Cloudflare fronts Render, in front of Vercel, so four entries arrive and only
        /// the leftmost (the real client) is stable — the rest rotate per request. This
        /// is the case the whole feature exists to get right, and the case that was
        /// silently broken in production at TrustedProxyHopCount=0.
        /// </summary>
        [Fact]
        public void Resolve_ForTheMeasuredProductionChain_LandsOnTheRealClient()
        {
            var context = CreateContext(
                "127.0.0.1",
                forwardedFor: "41.44.94.175,3.68.89.111, 172.70.243.46, 10.30.34.239");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 3 };

            ClientIpResolver.Resolve(context, settings).Should().Be("41.44.94.175");
        }

        [Fact]
        public void Resolve_ForTheMeasuredProductionChain_AtHopCountZero_PicksARotatingPrivateAddress()
        {
            // Regression guard for the actual outage: at 0 the resolver returned the
            // rightmost entry, a Render-internal 10.x that differs between requests, so
            // every request got its own bucket and rate limiting never fired at all.
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0 };

            var first = CreateContext("127.0.0.1", forwardedFor: "41.44.94.175,3.68.89.111, 172.70.243.46, 10.30.34.239");
            var second = CreateContext("127.0.0.1", forwardedFor: "41.44.94.175,3.70.131.168, 172.71.172.44, 10.24.202.146");

            ClientIpResolver.Resolve(first, settings)
                .Should().NotBe(ClientIpResolver.Resolve(second, settings),
                    "hop count 0 partitions on the rotating Render-internal entry — this is the bug, asserted so it can't come back unnoticed");
        }

        [Fact]
        public void Resolve_ForTheMeasuredProductionChain_IsStableForOneClientAcrossRotatingInfrastructure()
        {
            // The property that makes rate limiting work: same client, different
            // infrastructure entries, same bucket.
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 3 };

            var first = CreateContext("127.0.0.1", forwardedFor: "41.44.94.175,3.68.89.111, 172.70.243.46, 10.30.34.239");
            var second = CreateContext("127.0.0.1", forwardedFor: "41.44.94.175,3.70.131.168, 172.71.172.44, 10.24.202.146");

            ClientIpResolver.Resolve(first, settings)
                .Should().Be(ClientIpResolver.Resolve(second, settings)).And.Be("41.44.94.175");
        }

        [Fact]
        public void Resolve_ForTheMeasuredProductionChain_IgnoresAPrependedForgedEntry()
        {
            // Counting from the right is what keeps this safe: a caller stuffing an
            // entry in front lengthens the chain without moving the trusted position.
            var context = CreateContext(
                "127.0.0.1",
                forwardedFor: "1.2.3.4, 41.44.94.175, 3.68.89.111, 172.70.243.46, 10.30.34.239");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 3 };

            ClientIpResolver.Resolve(context, settings).Should().Be("41.44.94.175");
        }

        // --- ClientIpHeader: opt-in only, and NOT used by this deployment. Every test
        // below sets it explicitly, because the default is now empty (see
        // ProxySettings.ClientIpHeader for why that belief was reversed).

        [Fact]
        public void Resolve_WhenNotTrustingForwardedFor_IgnoresRealIpHeaderToo()
        {
            // The local-dev/CI guarantee: with the master gate off, NO proxy-supplied
            // header is consulted, including the new one.
            var context = CreateContext("203.0.113.9", realIp: "198.51.100.5");
            var settings = new ProxySettings { TrustForwardedFor = false };

            ClientIpResolver.Resolve(context, settings).Should().Be("203.0.113.9");
        }

        [Fact]
        public void Resolve_PrefersRealIpHeader_OverForwardedForAndSocketPeer()
        {
            // The production shape this fix exists for: Vercel puts the real client in
            // X-Real-IP and leaves it out of X-Forwarded-For entirely, so the XFF chain
            // holds only infrastructure addresses.
            var context = CreateContext("10.0.0.1", forwardedFor: "203.0.113.77", realIp: "198.51.100.5");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0, ClientIpHeader = "X-Real-IP" };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }

        [Fact]
        public void Resolve_DistinctRealClients_MapToDistinctPartitionKeys_ViaRealIpHeader()
        {
            // The property the whole change is for, restated for the X-Real-IP path:
            // two users behind the same Vercel egress must not share a bucket.
            var settings = new ProxySettings { TrustForwardedFor = true, ClientIpHeader = "X-Real-IP" };

            var userA = CreateContext("10.0.0.1", forwardedFor: "203.0.113.77", realIp: "198.51.100.5");
            var userB = CreateContext("10.0.0.1", forwardedFor: "203.0.113.77", realIp: "198.51.100.9");

            ClientIpResolver.Resolve(userA, settings)
                .Should().NotBe(ClientIpResolver.Resolve(userB, settings));
        }

        [Fact]
        public void Resolve_WhenRealIpHeaderAbsent_FallsBackToForwardedForPositionalLogic()
        {
            var context = CreateContext("10.0.0.1", forwardedFor: "198.51.100.5, 203.0.113.77");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 1 };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }

        [Fact]
        public void Resolve_WhenRealIpHeaderUnparseable_FallsBackToForwardedFor()
        {
            var context = CreateContext("10.0.0.1", forwardedFor: "198.51.100.5", realIp: "not-an-ip");
            var settings = new ProxySettings { TrustForwardedFor = true, TrustedProxyHopCount = 0, ClientIpHeader = "X-Real-IP" };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }

        [Fact]
        public void Resolve_WhenRealIpHeaderUnparseableAndNoForwardedFor_FailsClosedToSocketPeer()
        {
            var context = CreateContext("10.0.0.1", realIp: "not-an-ip");
            var settings = new ProxySettings { TrustForwardedFor = true, ClientIpHeader = "X-Real-IP" };

            ClientIpResolver.Resolve(context, settings).Should().Be("10.0.0.1");
        }

        [Fact]
        public void Resolve_WithClientIpHeaderDisabled_IgnoresRealIp_AndUsesForwardedForOnly()
        {
            // The escape hatch: if X-Real-IP ever turns out to be caller-settable on
            // this deployment, blanking the setting reverts to the right-anchored read.
            var context = CreateContext("10.0.0.1", forwardedFor: "198.51.100.5", realIp: "1.2.3.4");
            var settings = new ProxySettings
            {
                TrustForwardedFor = true,
                TrustedProxyHopCount = 0,
                ClientIpHeader = string.Empty
            };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }

        [Fact]
        public void Resolve_WhenRealIpHeaderAppendedRatherThanOverwritten_TakesTheRightmostEntry()
        {
            // X-Real-IP is defined as single-valued, so this is anomalous — but if some
            // hop appends instead of overwriting, the nearest hop's contribution (the
            // rightmost) is the only one attributable, same rule as X-Forwarded-For.
            var context = CreateContext("10.0.0.1");
            context.Request.Headers.Append("X-Real-IP", "1.2.3.4");
            context.Request.Headers.Append("X-Real-IP", "198.51.100.5");
            var settings = new ProxySettings { TrustForwardedFor = true, ClientIpHeader = "X-Real-IP" };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }

        [Fact]
        public void Resolve_StripsPortsFromRealIpHeader()
        {
            var context = CreateContext("10.0.0.1", realIp: "198.51.100.5:54321");
            var settings = new ProxySettings { TrustForwardedFor = true, ClientIpHeader = "X-Real-IP" };

            ClientIpResolver.Resolve(context, settings).Should().Be("198.51.100.5");
        }
    }
}
