using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.GetPublishedCourses
{
    public class GetPublishedCoursesQueryHandler
        : IRequestHandler<GetPublishedCoursesQuery, PagedResult<CourseListDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetPublishedCoursesQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<CourseListDto>> Handle(
            GetPublishedCoursesQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Courses
                .AsNoTracking()
                .Where(x => x.Status == CourseStatuses.Published);

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                var category = request.Category.Trim().ToLower();
                query = query.Where(x => x.Category != null && x.Category.ToLower() == category);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(search) ||
                    x.Slug.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var courses = await query
                .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var courseIds = courses.Select(x => x.Id).ToList();

            // One set-based query for the whole page — the active-enrollment count is a
            // correlated subquery EF translates into the same statement, not a
            // per-cohort round trip. Contrast GetPublishedCourseDetailQueryHandler, which
            // loops per cohort for a single course; that pattern doesn't scale to a page
            // of courses.
            var candidates = await _context.Cohorts
                .AsNoTracking()
                .Where(c => courseIds.Contains(c.CourseId) && c.Status == CohortStatuses.Open)
                .Select(c => new
                {
                    c.CourseId,
                    Candidate = new NextCohortSelector.Candidate(
                        c.Id,
                        c.Name,
                        c.StartDate,
                        c.EnrollmentCutoffDate,
                        c.Capacity,
                        c.Enrollments.Count(e => e.Status == EnrollmentStatuses.Active))
                })
                .ToListAsync(cancellationToken);

            var candidatesByCourse = candidates.ToLookup(x => x.CourseId, x => x.Candidate);
            var now = DateTime.UtcNow;

            var items = courses
                .Select(x => CourseMapping.ToListDto(
                    x,
                    NextCohortSelector.Select(candidatesByCourse[x.Id], now)))
                .ToList();

            return new PagedResult<CourseListDto>(items, request.Page, request.PageSize, totalCount);
        }
    }
}
