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
        public void WriteAuthCookies_SetsBothCookies_AsHttpOnlySecureLaxHostOnly()
        {
            var context = new DefaultHttpContext();
            CreateSut().WriteAuthCookies(context.Response, CreateAuth());

            var setCookieHeaders = context.Response.Headers.SetCookie.OfType<string>().ToArray();
            setCookieHeaders.Should().HaveCount(2);

            foreach (var header in setCookieHeaders)
            {
                var lower = header.ToLowerInvariant();
                lower.Should().Contain("httponly");
                lower.Should().Contain("secure");
                lower.Should().Contain("samesite=lax");
                lower.Should().Contain("path=/");
                lower.Should().NotContain("domain=");
            }

            setCookieHeaders.Should().Contain(h => h.StartsWith($"{AuthCookieWriter.AccessTokenCookieName}=access-token-value"));
            setCookieHeaders.Should().Contain(h => h.StartsWith($"{AuthCookieWriter.RefreshTokenCookieName}=refresh-token-value"));
        }

        [Fact]
        public void ClearAuthCookies_ExpiresBothCookies_InThePast()
        {
            var context = new DefaultHttpContext();
            CreateSut().ClearAuthCookies(context.Response);

            var setCookieHeaders = context.Response.Headers.SetCookie.OfType<string>().ToArray();
            setCookieHeaders.Should().HaveCount(2);
            setCookieHeaders.Should().OnlyContain(h => h.Contains("expires=Thu, 01 Jan 1970"));
        }
    }
}
