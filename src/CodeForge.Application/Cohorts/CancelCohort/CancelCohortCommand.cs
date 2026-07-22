using CodeForge.Application.Cohorts.Common;
using MediatR;

namespace CodeForge.Application.Cohorts.CancelCohort
{
    public record CancelCohortCommand(Guid Id) : IRequest<CohortMutationResultDto>;
}
