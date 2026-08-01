using CodeForge.Api.Authentication;
using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeForge.UnitTests.Authentication
{
    public class AuthCookieWriterTests
    {
        private static AuthCookieWriter CreateSut() => new(Options.Create(new JwtSettings
        {
            Secret = "unit_test_secret_key_that_is_long_enough_1234567890",
            Issuer = "CodeForgeTests",
            Audience = "CodeForgeTestsUsers",
            ExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        }));

        private static AuthResponse CreateAuth() => new(
            Guid.NewGuid(), "student@codeforge.academy", "Test Student", "student",
            "access-token-value", "refresh-token-value", DateTime.UtcNow.AddDays(7), false);

        [Fact]
        public void WriteAuthCookies_SetsAllThreeCookies_WithCorrectFlags()
        {
            var context = new DefaultHttpContext();
            CreateSut().WriteAuthCookies(context.Response, CreateAuth());

            var setCookieHeaders = context.Response.Headers.SetCookie.OfType<string>().ToArray();
            setCookieHeaders.Should().HaveCount(3);

            foreach (var header in setCookieHeaders)
            {
                var lower = header.ToLowerInvariant();
                lower.Should().Contain("secure");
                lower.Should().Contain("samesite=lax");
                lower.Should().Contain("path=/");
                lower.Should().NotContain("domain=");
            }

            var accessHeader = setCookieHeaders.Single(h => h.StartsWith($"{AuthCookieWriter.AccessTokenCookieName}=access-token-value"));
            accessHeader.ToLowerInvariant().Should().Contain("httponly");

            var refreshHeader = setCookieHeaders.Single(h => h.StartsWith($"{AuthCookieWriter.RefreshTokenCookieName}=refresh-token-value"));
            refreshHeader.ToLowerInvariant().Should().Contain("httponly");

            var csrfHeader = setCookieHeaders.Single(h => h.StartsWith($"{AuthCookieWriter.CsrfCookieName}="));
            csrfHeader.ToLowerInvariant().Should().NotContain("httponly");
        }

        [Fact]
        public void ClearAuthCookies_ExpiresAllThreeCookies_InThePast()
        {
            var context = new DefaultHttpContext();
            CreateSut().ClearAuthCookies(context.Response);

            var setCookieHeaders = context.Response.Headers.SetCookie.OfType<string>().ToArray();
            setCookieHeaders.Should().HaveCount(3);
            setCookieHeaders.Should().OnlyContain(h => h.Contains("expires=Thu, 01 Jan 1970"));
        }
    }
}
