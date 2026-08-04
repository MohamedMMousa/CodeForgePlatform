using System.Net;
using CodeForge.Api.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CodeForge.UnitTests.RateLimiting
{
    public class ClientIpResolverTests
    {
        private static HttpContext CreateContext(string? socketPeer, string? forwardedFor = null)
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
            // The production shape: [real-client, vercel-egress] — TrustedProxyHopCount=1
            // is what skips past the Render-edge-appended "vercel-egress" entry to reach
            // the one Vercel itself wrote for the real client.
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
    }
}
