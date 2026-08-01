using CodeForge.Application.Authentication.ChangePassword;
using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Authentication.ForgotPassword;
using CodeForge.Application.Authentication.GetCurrentUser;
using CodeForge.Application.Authentication.Login;
using CodeForge.Application.Authentication.RefreshToken;
using CodeForge.Application.Authentication.ResetPassword;
using CodeForge.Api.Authentication;
using CodeForge.Api.Filters;
using CodeForge.Api.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly AuthCookieWriter _cookieWriter;

        public AuthController(ISender sender, AuthCookieWriter cookieWriter)
        {
            _sender = sender;
            _cookieWriter = cookieWriter;
        }

        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequestWithCookies(
                new LoginCommand(request.Email, request.Password),
                cancellationToken);
        }

        [HttpPost("refresh-token")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest? request, CancellationToken cancellationToken)
        {
            // The cookie is the primary source; the body is accepted only as a fallback
            // for as long as any caller still sends one (dropped once the frontend moves
            // fully to cookie-only refresh).
            var refreshToken = Request.Cookies[AuthCookieWriter.RefreshTokenCookieName] ?? request?.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            return await SendAuthRequestWithCookies(
                new RefreshTokenCommand(refreshToken),
                cancellationToken);
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [ProducesResponseType(typeof(AuthMessageResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequest(
                new ForgotPasswordCommand(request.Email),
                cancellationToken);
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [ProducesResponseType(typeof(AuthMessageResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequest(
                new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
                cancellationToken);
        }

        [Authorize]
        [AllowPendingPasswordChange]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequestWithCookies(
                new ChangePasswordCommand(request.CurrentPassword, request.NewPassword),
                cancellationToken);
        }

        [Authorize]
        [AllowPendingPasswordChange]
        [HttpGet("me")]
        [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            return await SendAuthRequest(new GetCurrentUserQuery(), cancellationToken);
        }

        private async Task<IActionResult> SendAuthRequest<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            // Exceptions are translated centrally by ExceptionHandlingMiddleware.
            var response = await _sender.Send(request, cancellationToken);
            return Ok(response);
        }

        /// <summary>
        /// Same as SendAuthRequest, for the three actions whose result is an
        /// AuthResponse: also mints the httpOnly session cookies from it.
        /// </summary>
        private async Task<IActionResult> SendAuthRequestWithCookies(
            IRequest<AuthResponse> request,
            CancellationToken cancellationToken)
        {
            var response = await _sender.Send(request, cancellationToken);
            _cookieWriter.WriteAuthCookies(Response, response);
            return Ok(response);
        }

        public record LoginRequest(string Email, string Password);
        public record RefreshTokenRequest(string? RefreshToken);
        public record ForgotPasswordRequest(string Email);
        public record ResetPasswordRequest(string Email, string Token, string NewPassword);
        public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    }
}
