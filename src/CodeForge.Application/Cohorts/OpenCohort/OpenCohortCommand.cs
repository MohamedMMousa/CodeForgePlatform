using CodeForge.Application.Cohorts.Common;
using MediatR;

namespace CodeForge.Application.Cohorts.OpenCohort
{
    public record OpenCohortCommand(Guid Id) : IRequest<CohortMutationResultDto>;
}
