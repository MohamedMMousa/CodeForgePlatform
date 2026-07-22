using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.SubmitAssignment
{
    public record SubmitAssignmentCommand(Guid AssignmentId, string Code) : IRequest<SubmissionResultDto>;
}
