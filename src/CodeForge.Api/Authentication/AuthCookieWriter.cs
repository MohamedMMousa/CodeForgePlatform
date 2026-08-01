using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace CodeForge.Api.Authentication
{
    /// <summary>
    /// Single place that owns the auth cookies' names, flags, and expiry so the two
    /// never drift apart across the controller actions that issue them. All cookies are
    /// host-only (no Domain=) and Path=/ — see ARCHITECTURE.md §3 for why the refresh
    /// cookie is not scoped narrower despite being the more sensitive of the two.
    /// </summary>
    public class AuthCookieWriter
    {
        public const string AccessTokenCookieName = "cf_access";
        public const string RefreshTokenCookieName = "cf_refresh";

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

            response.Cookies.Append(AccessTokenCookieName, auth.AccessToken, BuildOptions(accessTokenExpires));
            response.Cookies.Append(RefreshTokenCookieName, auth.RefreshToken, BuildOptions(refreshTokenExpires));
        }

        public void ClearAuthCookies(HttpResponse response)
        {
            var expired = BuildOptions(DateTimeOffset.UnixEpoch);
            response.Cookies.Delete(AccessTokenCookieName, expired);
            response.Cookies.Delete(RefreshTokenCookieName, expired);
        }

        private static CookieOptions BuildOptions(DateTimeOffset expires) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expires
        };
    }
}
