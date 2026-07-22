using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.GradeSubmission
{
    public record GradeSubmissionCommand(Guid SubmissionId, int ManualScore, string? ManualFeedback) : IRequest<SubmissionResultDto>;
}
