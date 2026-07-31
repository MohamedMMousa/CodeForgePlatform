using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CodeForge.Api.Filters
{
    /// <summary>
    /// Global authorization filter: rejects every request from an authenticated user
    /// whose token carries must_change_password=true, unless the endpoint is marked
    /// [AllowAnonymous] or [AllowPendingPasswordChange]. Registered globally in
    /// Program.cs so a new endpoint is protected the moment it's written — no
    /// per-endpoint opt-in required, only an explicit opt-out.
    /// </summary>
    public class PasswordChangeRequiredFilter : IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var endpointMetadata = context.ActionDescriptor.EndpointMetadata;

            var isExempt = endpointMetadata.Any(m =>
                m is AllowAnonymousAttribute ||
                m is AllowPendingPasswordChangeAttribute);

            if (isExempt)
            {
                return Task.CompletedTask;
            }

            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            var mustChangePassword = user.FindFirst(CustomClaimTypes.MustChangePassword)?.Value;
            if (mustChangePassword == "true")
            {
                throw new PasswordChangeRequiredException();
            }

            return Task.CompletedTask;
        }
    }
}
