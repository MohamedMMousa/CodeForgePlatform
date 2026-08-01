using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Authentication.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AuthMessageResponse>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public ResetPasswordCommandHandler(
            ICodeForgeDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthMessageResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var now = DateTime.UtcNow;
            var hashedToken = _jwtTokenGenerator.HashToken(request.Token);

            var resetToken = await _context.PasswordResetTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Token == hashedToken &&
                         x.UsedAt == null &&
                         x.ExpiresAt > now &&
                         x.User.Email.ToLower() == normalizedEmail,
                    cancellationToken);

            if (resetToken is null || !resetToken.User.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid or expired password reset token.");
            }

            resetToken.User.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            resetToken.User.MustChangePassword = false;
            resetToken.User.RefreshToken = null;
            resetToken.User.RefreshTokenExpiryTime = null;
            resetToken.User.PreviousRefreshToken = null;
            resetToken.User.PendingRefreshToken = null;
            resetToken.User.RefreshTokenRotatedAt = null;
            resetToken.UsedAt = now;

            await _context.SaveChangesAsync(cancellationToken);

            return new AuthMessageResponse("Password has been reset.");
        }
    }
}
