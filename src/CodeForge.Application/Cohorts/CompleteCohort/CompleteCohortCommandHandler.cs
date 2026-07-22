using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Cohorts.CompleteCohort
{
    public class CompleteCohortCommandHandler : IRequestHandler<CompleteCohortCommand, CohortMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CompleteCohortCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CohortMutationResultDto> Handle(CompleteCohortCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var cohort = await _context.Cohorts.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (cohort is null)
            {
                throw new KeyNotFoundException("Batch was not found.");
            }

            if (cohort.Status != CohortStatuses.Open)
            {
                throw new InvalidOperationException("Only an open batch can be marked completed.");
            }

            cohort.Status = CohortStatuses.Completed;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "cohort.completed", nameof(Cohort), cohort.Id, new { cohort.Name }));

            await _context.SaveChangesAsync(cancellationToken);

            return new CohortMutationResultDto(cohort.Id, "Batch marked completed.");
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
