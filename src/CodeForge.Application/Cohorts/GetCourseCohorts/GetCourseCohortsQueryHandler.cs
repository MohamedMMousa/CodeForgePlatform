using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Cohorts.GetCourseCohorts
{
    public class GetCourseCohortsQueryHandler : IRequestHandler<GetCourseCohortsQuery, PagedResult<CohortListDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCourseCohortsQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<CohortListDto>> Handle(
            GetCourseCohortsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Cohorts
                .AsNoTracking()
                .Include(x => x.Course)
                .Where(x => x.CourseId == request.CourseId);

            var totalCount = await query.CountAsync(cancellationToken);

            var cohorts = await query
                .OrderByDescending(x => x.StartDate).ThenBy(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var items = new List<CohortListDto>();

            foreach (var cohort in cohorts)
            {
                var enrolledCount = await CohortAvailability.GetActiveEnrollmentCountAsync(
                    _context, cohort.Id, cancellationToken);
                items.Add(CohortMapping.ToDto(cohort, enrolledCount, now));
            }

            return new PagedResult<CohortListDto>(items, request.Page, request.PageSize, totalCount);
        }
    }
}
