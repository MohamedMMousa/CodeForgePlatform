using CodeForge.Application.Authentication.ChangePassword;
using CodeForge.Application.Authentication.ForgotPassword;
using CodeForge.Application.Authentication.GetCurrentUser;
using CodeForge.Application.Authentication.Login;
using CodeForge.Application.Authentication.RefreshToken;
using CodeForge.Application.Authentication.ResetPassword;
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

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequest(
                new LoginCommand(request.Email, request.Password),
                cancellationToken);
        }

        [HttpPost("refresh-token")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequest(
                new RefreshTokenCommand(request.RefreshToken),
                cancellationToken);
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequest(
                new ForgotPasswordCommand(request.Email),
                cancellationToken);
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequest(
                new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
                cancellationToken);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            return await SendAuthRequest(
                new ChangePasswordCommand(request.CurrentPassword, request.NewPassword),
                cancellationToken);
        }

        [Authorize]
        [HttpGet("me")]
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

        public record LoginRequest(string Email, string Password);
        public record RefreshTokenRequest(string RefreshToken);
        public record ForgotPasswordRequest(string Email);
        public record ResetPasswordRequest(string Email, string Token, string NewPassword);
        public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    }
}
