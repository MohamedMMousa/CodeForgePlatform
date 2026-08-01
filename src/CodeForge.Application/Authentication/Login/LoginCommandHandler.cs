using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeForge.Application.Authentication.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly JwtSettings _jwtSettings;

        public LoginCommandHandler(
            ICodeForgeDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var normalizedEmail = request.Email.Trim().ToLower();
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            if (user is null ||
                !user.IsActive ||
                !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

            // Persist only the hash; the plaintext token is returned to the client.
            // A fresh login starts a new rotation lineage — any grace-window state
            // left over from a prior session is stale and must not carry forward.
            user.RefreshToken = _jwtTokenGenerator.HashToken(refreshToken);
            user.RefreshTokenExpiryTime = refreshTokenExpiresAt;
            user.PreviousRefreshToken = null;
            user.PendingRefreshToken = null;
            user.RefreshTokenRotatedAt = null;

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
