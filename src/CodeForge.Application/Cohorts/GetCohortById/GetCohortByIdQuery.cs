using CodeForge.Application.Cohorts.Common;
using MediatR;

namespace CodeForge.Application.Cohorts.GetCohortById
{
    public record GetCohortByIdQuery(Guid Id) : IRequest<CohortListDto>;
}
