using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeForge.Application.Authentication.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, AuthMessageResponse>
    {
        // Constant response so callers cannot probe which emails are registered.
        private const string GenericMessage =
            "If an account exists for that email, a password reset link has been sent.";

        private readonly ICodeForgeDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailSender _emailSender;
        private readonly EmailSettings _emailSettings;

        public ForgotPasswordCommandHandler(
            ICodeForgeDbContext context,
            IJwtTokenGenerator jwtTokenGenerator,
            IEmailSender emailSender,
            IOptions<EmailSettings> emailOptions)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailSender = emailSender;
            _emailSettings = emailOptions.Value;
        }

        public async Task<AuthMessageResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            if (user is null || !user.IsActive)
            {
                return new AuthMessageResponse(GenericMessage);
            }

            var resetToken = _jwtTokenGenerator.GenerateRefreshToken();
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                // Persist only the hash; the plaintext token travels to the user by email.
                Token = _jwtTokenGenerator.HashToken(resetToken),
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            await _context.SaveChangesAsync(cancellationToken);

            var resetLink =
                $"{_emailSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password" +
                $"?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(resetToken)}";

            var body =
                $"<p>Hello {user.FullName},</p>" +
                "<p>We received a request to reset your CodeForge Academy password. " +
                $"Click the link below to set a new password (valid for 1 hour):</p>" +
                $"<p><a href=\"{resetLink}\">Reset your password</a></p>" +
                "<p>If you did not request this, you can safely ignore this email.</p>";

            await _emailSender.SendAsync(user.Email, "Reset your CodeForge Academy password", body, cancellationToken);

            return new AuthMessageResponse(GenericMessage);
        }
    }
}
