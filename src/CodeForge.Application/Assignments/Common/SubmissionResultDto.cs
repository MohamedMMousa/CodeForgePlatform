namespace CodeForge.Application.Assignments.Common
{
    public record TestResultDto(Guid TestCaseId, bool IsHidden, bool Passed, string? ActualOutput, string? ErrorMessage, int? ExecutionTimeMs);

    public record SubmissionResultDto(
        Guid SubmissionId,
        int AttemptNumber,
        DateTime SubmittedAt,
        bool IsLate,
        int? AutoScore,
        string AutoGradingStatus,
        int? ManualScore,
        string? ManualFeedback,
        int? FinalScore,
        List<TestResultDto> TestResults);
}
