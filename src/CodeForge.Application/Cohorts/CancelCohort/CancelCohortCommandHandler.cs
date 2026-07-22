using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Cohorts.CancelCohort
{
    public class CancelCohortCommandHandler : IRequestHandler<CancelCohortCommand, CohortMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CancelCohortCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CohortMutationResultDto> Handle(CancelCohortCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var cohort = await _context.Cohorts.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (cohort is null)
            {
                throw new KeyNotFoundException("Batch was not found.");
            }

            if (cohort.Status == CohortStatuses.Completed)
            {
                throw new InvalidOperationException("A completed batch cannot be cancelled.");
            }

            cohort.Status = CohortStatuses.Cancelled;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "cohort.cancelled", nameof(Cohort), cohort.Id, new { cohort.Name }));

            await _context.SaveChangesAsync(cancellationToken);

            return new CohortMutationResultDto(cohort.Id, "Batch cancelled.");
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return userId;
        }
    }
}
