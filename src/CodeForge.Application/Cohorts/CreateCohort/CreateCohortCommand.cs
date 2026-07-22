using CodeForge.Application.Cohorts.Common;
using MediatR;

namespace CodeForge.Application.Cohorts.CreateCohort
{
    public record CreateCohortCommand(
        Guid CourseId,
        string Name,
        DateTime StartDate,
        DateTime EndDate,
        DateTime EnrollmentCutoffDate,
        int Capacity,
        int GracePeriodDays) : IRequest<CohortListDto>;
}
