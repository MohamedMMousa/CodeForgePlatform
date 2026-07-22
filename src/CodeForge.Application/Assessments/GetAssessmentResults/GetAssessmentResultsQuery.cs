using MediatR;

namespace CodeForge.Application.Assessments.GetAssessmentResults
{
    public record StudentAttemptDto(
        Guid AttemptId,
        Guid StudentId,
        string StudentName,
        int AttemptNumber,
        int? Score,
        bool? Passed,
        DateTime StartedAt,
        DateTime? SubmittedAt);

    public record AssessmentResultsDto(Guid AssessmentId, string AssessmentTitle, List<StudentAttemptDto> Attempts);

    public record GetAssessmentResultsQuery(Guid AssessmentId) : IRequest<AssessmentResultsDto>;
}
