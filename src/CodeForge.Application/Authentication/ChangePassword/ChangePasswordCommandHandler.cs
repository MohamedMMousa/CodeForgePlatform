using CodeForge.Application.Authentication.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Authentication.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthMessageResponse>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordHasher _passwordHasher;

        public ChangePasswordCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _currentUserService = currentUserService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthMessageResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
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
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync(cancellationToken);

            return new AuthMessageResponse("Password has been changed.");
        }
    }
}
