using CodeForge.Application.Announcements.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Announcements.GetAnnouncements
{
    public class GetAnnouncementsQueryHandler : IRequestHandler<GetAnnouncementsQuery, PagedResult<AnnouncementDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAnnouncementsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResult<AnnouncementDto>> Handle(
            GetAnnouncementsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            IQueryable<Domain.Entities.Announcement> query = _context.Announcements
                .AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.Author);

            if (request.CourseId.HasValue)
            {
                var course = await _context.Courses
                    .Include(c => c.Instructors)
                    .Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.Id == request.CourseId.Value, cancellationToken);

                if (course is null)
                {
                    throw new KeyNotFoundException("Course was not found.");
                }

                CourseContentAuthorization.EnsureCanView(_currentUserService, course, currentUserId);

                query = query.Where(a => a.CourseId == request.CourseId.Value || a.CourseId == null);
            }
            else
            {
                query = query.Where(a => a.CourseId == null);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var announcements = await query
                .OrderByDescending(a => a.CreatedAt).ThenBy(a => a.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = announcements.Select(AnnouncementMapping.ToDto).ToList();

            return new PagedResult<AnnouncementDto>(items, request.Page, request.PageSize, totalCount);
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
