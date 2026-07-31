using System.Security.Claims;
using CodeForge.Api.Filters;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace CodeForge.UnitTests.Filters
{
    public class PasswordChangeRequiredFilterTests
    {
        private static ClaimsPrincipal AuthenticatedUser(string? mustChangePassword)
        {
            var claims = new List<Claim>();
            if (mustChangePassword is not null)
            {
                claims.Add(new Claim(CustomClaimTypes.MustChangePassword, mustChangePassword));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
        }

        private static ClaimsPrincipal AnonymousUser() => new(new ClaimsIdentity());

        private static AuthorizationFilterContext CreateContext(
            ClaimsPrincipal user, IReadOnlyList<object>? endpointMetadata = null)
        {
            var httpContext = new DefaultHttpContext { User = user };
            var actionDescriptor = new ActionDescriptor
            {
                EndpointMetadata = endpointMetadata?.ToList() ?? new List<object>()
            };
            var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        [Fact]
        public async Task Throws_WhenAuthenticatedUserMustChangePassword()
        {
            var sut = new PasswordChangeRequiredFilter();
            var context = CreateContext(AuthenticatedUser("true"));

            var act = () => sut.OnAuthorizationAsync(context);

            await act.Should().ThrowAsync<PasswordChangeRequiredException>();
        }

        [Fact]
        public async Task DoesNotThrow_WhenClaimIsFalse()
        {
            var sut = new PasswordChangeRequiredFilter();
            var context = CreateContext(AuthenticatedUser("false"));

            await sut.OnAuthorizationAsync(context);
        }

        [Fact]
        public async Task DoesNotThrow_WhenClaimIsAbsent()
        {
            var sut = new PasswordChangeRequiredFilter();
            var context = CreateContext(AuthenticatedUser(null));

            await sut.OnAuthorizationAsync(context);
        }

        [Fact]
        public async Task DoesNotThrow_WhenUserIsNotAuthenticated()
        {
            var sut = new PasswordChangeRequiredFilter();
            var context = CreateContext(AnonymousUser());

            await sut.OnAuthorizationAsync(context);
        }

        [Fact]
        public async Task DoesNotThrow_WhenEndpointIsAllowAnonymous()
        {
            var sut = new PasswordChangeRequiredFilter();
            var context = CreateContext(
                AuthenticatedUser("true"),
                new object[] { new AllowAnonymousAttribute() });

            await sut.OnAuthorizationAsync(context);
        }

        [Fact]
        public async Task DoesNotThrow_WhenEndpointAllowsPendingPasswordChange()
        {
            var sut = new PasswordChangeRequiredFilter();
            var context = CreateContext(
                AuthenticatedUser("true"),
                new object[] { new AllowPendingPasswordChangeAttribute() });

            await sut.OnAuthorizationAsync(context);
        }
    }
}
