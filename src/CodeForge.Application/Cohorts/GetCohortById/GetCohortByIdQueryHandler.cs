using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Cohorts.GetCohortById
{
    public class GetCohortByIdQueryHandler : IRequestHandler<GetCohortByIdQuery, CohortListDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCohortByIdQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<CohortListDto> Handle(GetCohortByIdQuery request, CancellationToken cancellationToken)
        {
            var cohort = await _context.Cohorts
                .AsNoTracking()
                .Include(x => x.Course)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (cohort is null)
            {
                throw new KeyNotFoundException("Batch was not found.");
            }

            var enrolledCount = await CohortAvailability.GetActiveEnrollmentCountAsync(
                _context, cohort.Id, cancellationToken);

            return CohortMapping.ToDto(cohort, enrolledCount, DateTime.UtcNow);
        }
    }
}
