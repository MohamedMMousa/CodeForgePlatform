using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeForge.Application.Authentication.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthResponse>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly JwtSettings _jwtSettings;

        public ChangePasswordCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _currentUserService = currentUserService;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<AuthResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated user could not be resolved.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Authenticated user could not be resolved.");
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Current password is incorrect.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.MustChangePassword = false;

            // Rotate rather than clear: the caller's current access token still carries
            // must_change_password=true (it was baked in at issue time), so a fresh pair
            // is minted here to let them resume normal access immediately, without a
            // second login.
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
            user.RefreshToken = _jwtTokenGenerator.HashToken(refreshToken);
            user.RefreshTokenExpiryTime = refreshTokenExpiresAt;

            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponse(
                user.Id,
                user.Email,
                user.FullName,
                user.Role,
                _jwtTokenGenerator.GenerateToken(user),
                refreshToken,
                refreshTokenExpiresAt,
                user.MustChangePassword);
        }
    }
}
