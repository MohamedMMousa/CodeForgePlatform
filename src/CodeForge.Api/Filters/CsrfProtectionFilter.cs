using System.Security.Cryptography;
using System.Text;
using CodeForge.Api.Authentication;
using CodeForge.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CodeForge.Api.Filters
{
    /// <summary>
    /// Global authorization filter implementing the double-submit CSRF defense: an
    /// unsafe request that carries an auth cookie must echo the cf_csrf cookie value
    /// back in the X-CSRF-Token header, or it's rejected. Only triggers when an auth
    /// cookie is present — CSRF is a risk created by ambient cookie credentials, so
    /// there is nothing to forge on a request that doesn't carry one. That also keeps
    /// anonymous public POSTs (enrollment requests, leads) working unchanged. Checking
    /// the refresh cookie too (not just the access cookie) covers /auth/refresh-token
    /// and /auth/logout, which authenticate off it directly.
    /// </summary>
    public class CsrfProtectionFilter : IAsyncAuthorizationFilter
    {
        public const string HeaderName = "X-CSRF-Token";

        private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace
        };

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;

            if (SafeMethods.Contains(request.Method))
            {
                return Task.CompletedTask;
            }

            var hasAuthCookie =
                request.Cookies.ContainsKey(AuthCookieWriter.AccessTokenCookieName) ||
                request.Cookies.ContainsKey(AuthCookieWriter.RefreshTokenCookieName);

            if (!hasAuthCookie)
            {
                return Task.CompletedTask;
            }

            var cookieToken = request.Cookies[AuthCookieWriter.CsrfCookieName];
            var headerToken = request.Headers[HeaderName].ToString();

            if (!TokensMatch(cookieToken, headerToken))
            {
                throw new CsrfValidationException();
            }

            return Task.CompletedTask;
        }

        private static bool TokensMatch(string? cookieToken, string? headerToken)
        {
            if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken))
            {
                return false;
            }

            var cookieBytes = Encoding.UTF8.GetBytes(cookieToken);
            var headerBytes = Encoding.UTF8.GetBytes(headerToken);

            return cookieBytes.Length == headerBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(cookieBytes, headerBytes);
        }
    }
}
