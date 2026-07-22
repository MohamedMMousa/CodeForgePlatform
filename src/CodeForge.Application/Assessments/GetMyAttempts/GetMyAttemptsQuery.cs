using MediatR;

namespace CodeForge.Application.Assessments.GetMyAttempts
{
    public record AttemptSummaryDto(Guid AttemptId, int AttemptNumber, int? Score, bool? Passed, DateTime StartedAt, DateTime? SubmittedAt);

    public record GetMyAttemptsQuery(Guid AssessmentId) : IRequest<IReadOnlyList<AttemptSummaryDto>>;
}
