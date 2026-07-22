using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Cohorts.OpenCohort
{
    public class OpenCohortCommandHandler : IRequestHandler<OpenCohortCommand, CohortMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public OpenCohortCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CohortMutationResultDto> Handle(OpenCohortCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var cohort = await _context.Cohorts.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (cohort is null)
            {
                throw new KeyNotFoundException("Batch was not found.");
            }

            if (cohort.Status != CohortStatuses.Draft)
            {
                throw new InvalidOperationException("Only a draft batch can be opened.");
            }

            cohort.Status = CohortStatuses.Open;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "cohort.opened", nameof(Cohort), cohort.Id, new { cohort.Name }));

            await _context.SaveChangesAsync(cancellationToken);

            return new CohortMutationResultDto(cohort.Id, "Batch opened for enrollment.");
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
