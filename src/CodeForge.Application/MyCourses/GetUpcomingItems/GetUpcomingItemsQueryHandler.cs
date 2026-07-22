using CodeForge.Application.Announcements.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.MyCourses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.MyCourses.GetUpcomingItems
{
    public class GetUpcomingItemsQueryHandler : IRequestHandler<GetUpcomingItemsQuery, UpcomingItemsDto>
    {
        private const int MaxItems = 10;

        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetUpcomingItemsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<UpcomingItemsDto> Handle(GetUpcomingItemsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == currentUserId && e.Status == EnrollmentStatuses.Active)
                .Select(e => e.CourseId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var upcomingSessions = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Module).ThenInclude(m => m.Course)
                .Where(s =>
                    (s.Type == SessionTypes.Live || s.Type == SessionTypes.InPerson)
                    && s.ScheduledAt != null
                    && s.ScheduledAt >= now
                    && enrolledCourseIds.Contains(s.Module.CourseId))
                .OrderBy(s => s.ScheduledAt)
                .Take(MaxItems)
                .Select(s => new UpcomingSessionDto(
                    s.Id,
                    s.Module.CourseId,
                    s.Module.Course.Title,
                    s.Module.Title,
                    s.Type,
                    s.Title,
                    s.ScheduledAt!.Value,
                    s.JoinLink,
                    s.Location))
                .ToListAsync(cancellationToken);

            var recentAnnouncements = await _context.Announcements
                .AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.Author)
                .Where(a => a.CourseId == null || enrolledCourseIds.Contains(a.CourseId.Value))
                .OrderByDescending(a => a.CreatedAt)
                .Take(MaxItems)
                .ToListAsync(cancellationToken);

            return new UpcomingItemsDto(
                upcomingSessions,
                recentAnnouncements.Select(AnnouncementMapping.ToDto).ToList());
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
