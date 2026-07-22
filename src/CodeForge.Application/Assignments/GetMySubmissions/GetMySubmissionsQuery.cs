using MediatR;

namespace CodeForge.Application.Assignments.GetMySubmissions
{
    public record SubmissionSummaryDto(
        Guid SubmissionId,
        int AttemptNumber,
        DateTime SubmittedAt,
        bool IsLate,
        int? AutoScore,
        string AutoGradingStatus,
        int? ManualScore,
        int? FinalScore);

    public record GetMySubmissionsQuery(Guid AssignmentId) : IRequest<IReadOnlyList<SubmissionSummaryDto>>;
}
