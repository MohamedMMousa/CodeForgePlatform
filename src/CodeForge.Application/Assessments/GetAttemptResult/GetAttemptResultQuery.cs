using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.GetAttemptResult
{
    public record GetAttemptResultQuery(Guid AttemptId) : IRequest<AttemptResultDto>;
}
