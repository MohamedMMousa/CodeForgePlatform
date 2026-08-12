using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.MyCourses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.MyCourses.GetMyCourses
{
    public class GetMyCoursesQueryHandler : IRequestHandler<GetMyCoursesQuery, IReadOnlyList<MyCourseSummaryDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyCoursesQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<MyCourseSummaryDto>> Handle(GetMyCoursesQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();

            var rows = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == currentUserId
                         && e.Status == EnrollmentStatuses.Active
                         && e.Course.DeletedAt == null)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new MyCourseSummaryDto(
                    e.CourseId,
                    e.Course.Title,
                    e.Course.Description,
                    e.Cohort.Name,
                    e.Cohort.StartDate,
                    e.Cohort.EndDate))
                .ToListAsync(cancellationToken);

            return rows.GroupBy(r => r.CourseId).Select(g => g.First()).ToList();
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
