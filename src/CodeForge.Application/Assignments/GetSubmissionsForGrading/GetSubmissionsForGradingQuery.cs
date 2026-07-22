using MediatR;

namespace CodeForge.Application.Assignments.GetSubmissionsForGrading
{
    public record StudentSubmissionDto(
        Guid SubmissionId,
        Guid StudentId,
        string StudentName,
        int AttemptNumber,
        DateTime SubmittedAt,
        bool IsLate,
        int? AutoScore,
        string AutoGradingStatus,
        int? ManualScore,
        int? FinalScore);

    public record AssignmentSubmissionsDto(Guid AssignmentId, string AssignmentTitle, List<StudentSubmissionDto> Submissions);

    public record GetSubmissionsForGradingQuery(Guid AssignmentId) : IRequest<AssignmentSubmissionsDto>;
}
