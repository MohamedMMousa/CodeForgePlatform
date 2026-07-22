using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Cohorts.GetCourseCohorts
{
    public class GetCourseCohortsQueryHandler : IRequestHandler<GetCourseCohortsQuery, IReadOnlyList<CohortListDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCourseCohortsQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CohortListDto>> Handle(
            GetCourseCohortsQuery request,
            CancellationToken cancellationToken)
        {
            var cohorts = await _context.Cohorts
                .AsNoTracking()
                .Include(x => x.Course)
                .Where(x => x.CourseId == request.CourseId)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var result = new List<CohortListDto>();

            foreach (var cohort in cohorts)
            {
                var enrolledCount = await CohortAvailability.GetActiveEnrollmentCountAsync(
                    _context, cohort.Id, cancellationToken);
                result.Add(CohortMapping.ToDto(cohort, enrolledCount, now));
            }

            return result;
        }
    }
}
