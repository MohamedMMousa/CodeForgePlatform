using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Cohorts.UpdateCohort
{
    public class UpdateCohortCommandHandler : IRequestHandler<UpdateCohortCommand, CohortListDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateCohortCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CohortListDto> Handle(UpdateCohortCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var cohort = await _context.Cohorts
                .Include(x => x.Course)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (cohort is null)
            {
                throw new KeyNotFoundException("Batch was not found.");
            }

            if (cohort.Status is CohortStatuses.Cancelled or CohortStatuses.Completed)
            {
                throw new InvalidOperationException("A cancelled or completed batch cannot be edited.");
            }

            cohort.Name = request.Name.Trim();
            cohort.StartDate = request.StartDate;
            cohort.EndDate = request.EndDate;
            cohort.EnrollmentCutoffDate = request.EnrollmentCutoffDate;
            cohort.Capacity = request.Capacity;
            cohort.GracePeriodDays = request.GracePeriodDays;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "cohort.updated", nameof(Cohort), cohort.Id,
                new { cohort.Name, cohort.Capacity }));

            await _context.SaveChangesAsync(cancellationToken);

            var enrolledCount = await CohortAvailability.GetActiveEnrollmentCountAsync(
                _context, cohort.Id, cancellationToken);

            return CohortMapping.ToDto(cohort, enrolledCount, DateTime.UtcNow);
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
