using System.Security.Cryptography;
using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace CodeForge.Api.Authentication
{
    /// <summary>
    /// Single place that owns the auth cookies' names, flags, and expiry so they never
    /// drift apart across the controller actions that issue them. The access and
    /// refresh cookies are host-only (no Domain=) and Path=/ — see ARCHITECTURE.md §3
    /// for why the refresh cookie is not scoped narrower despite being the more
    /// sensitive of the two. cf_csrf is deliberately NOT HttpOnly: client JS reads it
    /// and echoes it back in X-CSRF-Token for CsrfProtectionFilter to check.
    /// </summary>
    public class AuthCookieWriter
    {
        public const string AccessTokenCookieName = "cf_access";
        public const string RefreshTokenCookieName = "cf_refresh";
        public const string CsrfCookieName = "cf_csrf";

        private readonly JwtSettings _jwtSettings;

        public AuthCookieWriter(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public void WriteAuthCookies(HttpResponse response, AuthResponse auth)
        {
            var accessTokenExpires = DateTimeOffset.UtcNow
                .AddMinutes(_jwtSettings.ExpiryMinutes)
                .AddSeconds(-30); // expire the cookie slightly ahead of the JWT itself
            var refreshTokenExpires = new DateTimeOffset(
                DateTime.SpecifyKind(auth.RefreshTokenExpiresAt, DateTimeKind.Utc));

            response.Cookies.Append(AccessTokenCookieName, auth.AccessToken, BuildOptions(accessTokenExpires, httpOnly: true));
            response.Cookies.Append(RefreshTokenCookieName, auth.RefreshToken, BuildOptions(refreshTokenExpires, httpOnly: true));
            response.Cookies.Append(CsrfCookieName, GenerateCsrfToken(), BuildOptions(refreshTokenExpires, httpOnly: false));
        }

        public void ClearAuthCookies(HttpResponse response)
        {
            response.Cookies.Delete(AccessTokenCookieName, BuildOptions(DateTimeOffset.UnixEpoch, httpOnly: true));
            response.Cookies.Delete(RefreshTokenCookieName, BuildOptions(DateTimeOffset.UnixEpoch, httpOnly: true));
            response.Cookies.Delete(CsrfCookieName, BuildOptions(DateTimeOffset.UnixEpoch, httpOnly: false));
        }

        private static CookieOptions BuildOptions(DateTimeOffset expires, bool httpOnly) => new()
        {
            HttpOnly = httpOnly,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expires
        };

        private static string GenerateCsrfToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}
