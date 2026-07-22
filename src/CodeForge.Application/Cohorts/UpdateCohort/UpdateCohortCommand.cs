using CodeForge.Application.Cohorts.Common;
using MediatR;

namespace CodeForge.Application.Cohorts.UpdateCohort
{
    public record UpdateCohortCommand(
        Guid Id,
        string Name,
        DateTime StartDate,
        DateTime EndDate,
        DateTime EnrollmentCutoffDate,
        int Capacity,
        int GracePeriodDays) : IRequest<CohortListDto>;
}
