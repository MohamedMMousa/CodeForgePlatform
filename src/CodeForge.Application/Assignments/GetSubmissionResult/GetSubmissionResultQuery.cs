using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.GetSubmissionResult
{
    public record GetSubmissionResultQuery(Guid SubmissionId) : IRequest<SubmissionResultDto>;
}
