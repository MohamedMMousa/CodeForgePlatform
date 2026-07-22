using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Users.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Users.DeactivateUser
{
    public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, UserDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeactivateUserCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<UserDto> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();

            if (request.UserId == adminId)
            {
                throw new InvalidOperationException("You cannot deactivate your own account.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException("User was not found.");
            }

            user.IsActive = false;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "user.deactivated", nameof(User), user.Id, new { user.Email }));

            await _context.SaveChangesAsync(cancellationToken);

            return UserMapping.ToDto(user);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return userId;
        }
    }
}
