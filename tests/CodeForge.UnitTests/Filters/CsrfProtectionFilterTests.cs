using CodeForge.Api.Authentication;
using CodeForge.Api.Filters;
using CodeForge.Application.Common.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace CodeForge.UnitTests.Filters
{
    public class CsrfProtectionFilterTests
    {
        private static AuthorizationFilterContext CreateContext(
            string method,
            IDictionary<string, string>? cookies = null,
            string? csrfHeader = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;

            if (cookies is not null)
            {
                var cookieHeader = string.Join("; ", cookies.Select(kv => $"{kv.Key}={kv.Value}"));
                httpContext.Request.Headers.Append("Cookie", cookieHeader);
            }

            if (csrfHeader is not null)
            {
                httpContext.Request.Headers.Append(CsrfProtectionFilter.HeaderName, new StringValues(csrfHeader));
            }

            var actionDescriptor = new ActionDescriptor();
            var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        [Fact]
        public async Task DoesNotThrow_ForSafeMethod_EvenWithMismatchedTokens()
        {
            var sut = new CsrfProtectionFilter();
            var context = CreateContext(
                "GET",
                cookies: new Dictionary<string, string>
                {
                    [AuthCookieWriter.AccessTokenCookieName] = "token",
                    [AuthCookieWriter.CsrfCookieName] = "csrf-value"
                },
                csrfHeader: "wrong-value");

            await sut.OnAuthorizationAsync(context);
        }

        [Fact]
        public async Task DoesNotThrow_WhenNoAuthCookiePresent()
        {
            var sut = new CsrfProtectionFilter();
            var context = CreateContext("POST");

            await sut.OnAuthorizationAsync(context);
        }

        [Fact]
        public async Task DoesNotThrow_WhenHeaderMatchesCookie()
        {
            var sut = new CsrfProtectionFilter();
            var context = CreateContext(
                "POST",
                cookies: new Dictionary<string, string>
                {
                    [AuthCookieWriter.AccessTokenCookieName] = "token",
                    [AuthCookieWriter.CsrfCookieName] = "matching-value"
                },
                csrfHeader: "matching-value");

            await sut.OnAuthorizationAsync(context);
        }

        [Fact]
        public async Task Throws_WhenHeaderMissing_ButAuthCookiePresent()
        {
            var sut = new CsrfProtectionFilter();
            var context = CreateContext(
                "POST",
                cookies: new Dictionary<string, string>
                {
                    [AuthCookieWriter.AccessTokenCookieName] = "token",
                    [AuthCookieWriter.CsrfCookieName] = "matching-value"
                });

            var act = () => sut.OnAuthorizationAsync(context);

            await act.Should().ThrowAsync<CsrfValidationException>();
        }

        [Fact]
        public async Task Throws_WhenHeaderDoesNotMatchCookie()
        {
            var sut = new CsrfProtectionFilter();
            var context = CreateContext(
                "POST",
                cookies: new Dictionary<string, string>
                {
                    [AuthCookieWriter.AccessTokenCookieName] = "token",
                    [AuthCookieWriter.CsrfCookieName] = "matching-value"
                },
                csrfHeader: "different-value");

            var act = () => sut.OnAuthorizationAsync(context);

            await act.Should().ThrowAsync<CsrfValidationException>();
        }

        [Fact]
        public async Task Enforces_WhenOnlyRefreshCookiePresent()
        {
            var sut = new CsrfProtectionFilter();
            var context = CreateContext(
                "POST",
                cookies: new Dictionary<string, string>
                {
                    [AuthCookieWriter.RefreshTokenCookieName] = "refresh-token",
                    [AuthCookieWriter.CsrfCookieName] = "matching-value"
                },
                csrfHeader: "wrong");

            var act = () => sut.OnAuthorizationAsync(context);

            await act.Should().ThrowAsync<CsrfValidationException>();
        }
    }
}
