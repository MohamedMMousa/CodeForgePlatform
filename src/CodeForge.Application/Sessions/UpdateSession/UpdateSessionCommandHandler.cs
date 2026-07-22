using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Sessions.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Sessions.UpdateSession
{
    public class UpdateSessionCommandHandler : IRequestHandler<UpdateSessionCommand, SessionResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateSessionCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SessionResponseDto> Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
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

            if (request.InstructorId.HasValue)
            {
                var instructor = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.InstructorId.Value, cancellationToken);
                if (instructor is null || !instructor.IsActive || instructor.Role != Roles.Instructor)
                {
                    throw new InvalidOperationException("Active instructor was not found.");
                }
            }

            session.Type = request.Type;
            session.Title = request.Title.Trim();
            session.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            session.ScheduledAt = request.ScheduledAt;
            session.DurationMinutes = request.DurationMinutes;
            session.JoinLink = string.IsNullOrWhiteSpace(request.JoinLink) ? null : request.JoinLink.Trim();
            session.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
            session.VideoUrl = string.IsNullOrWhiteSpace(request.VideoUrl) ? null : request.VideoUrl.Trim();
            session.InstructorId = request.InstructorId;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "session.updated", nameof(Session), session.Id, new { session.Title, session.Type }));

            await _context.SaveChangesAsync(cancellationToken);

            return new SessionResponseDto(session.Id, "Session updated.");
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
