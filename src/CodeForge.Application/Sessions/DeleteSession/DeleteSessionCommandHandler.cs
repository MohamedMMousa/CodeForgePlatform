using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Sessions.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Sessions.DeleteSession
{
    public class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand, SessionResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteSessionCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SessionResponseDto> Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var session = await _context.Sessions
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (session is null)
            {
                throw new KeyNotFoundException("Session was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, session.Module.Course, currentUserId);

            _context.Sessions.Remove(session);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "session.deleted", nameof(Session), session.Id, new { session.Title }));

            await _context.SaveChangesAsync(cancellationToken);

            return new SessionResponseDto(session.Id, "Session deleted.");
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated user could not be resolved.");
            }

            return userId;
        }
    }
}
