using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeForge.Application.Authentication.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly JwtSettings _jwtSettings;

        public RefreshTokenCommandHandler(
            ICodeForgeDbContext context,
            IJwtTokenGenerator jwtTokenGenerator,
            IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var hashedToken = _jwtTokenGenerator.HashToken(request.RefreshToken);
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.RefreshToken == hashedToken &&
                         x.RefreshTokenExpiryTime != null &&
                         x.RefreshTokenExpiryTime > now,
                    cancellationToken);

            if (user is null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
            var refreshTokenExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpiryDays);

            // Rotate: persist only the hash of the new token.
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
