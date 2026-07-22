using CodeForge.Application.Cohorts.Common;
using MediatR;

namespace CodeForge.Application.Cohorts.CompleteCohort
{
    public record CompleteCohortCommand(Guid Id) : IRequest<CohortMutationResultDto>;
}
