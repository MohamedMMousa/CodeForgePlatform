using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Sessions.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Sessions.CreateSession
{
    public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, SessionResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateSessionCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SessionResponseDto> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(m => m.Sessions)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            var instructorId = request.InstructorId;
            if (instructorId.HasValue)
            {
                var instructor = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == instructorId.Value, cancellationToken);
                if (instructor is null || !instructor.IsActive || instructor.Role != Roles.Instructor)
                {
                    throw new InvalidOperationException("Active instructor was not found.");
                }
            }
            else if (_currentUserService.Role == Roles.Instructor)
            {
                instructorId = currentUserId;
            }

            var maxOrder = module.Sessions.Count == 0 ? 0 : module.Sessions.Max(s => s.OrderIndex);

            var session = new Session
            {
                ModuleId = module.Id,
                Type = request.Type,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                OrderIndex = maxOrder + 1,
                ScheduledAt = request.ScheduledAt,
                DurationMinutes = request.DurationMinutes,
                JoinLink = string.IsNullOrWhiteSpace(request.JoinLink) ? null : request.JoinLink.Trim(),
                Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
                VideoUrl = string.IsNullOrWhiteSpace(request.VideoUrl) ? null : request.VideoUrl.Trim(),
                InstructorId = instructorId
            };

            _context.Sessions.Add(session);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "session.created", nameof(Session), session.Id,
                new { session.Title, session.Type, moduleId = module.Id }));

            await _context.SaveChangesAsync(cancellationToken);

            return new SessionResponseDto(session.Id, "Session created.");
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
