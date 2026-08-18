namespace CodeForge.Application.Gradebook.Common
{
    public record AssessmentGradeDto(Guid AssessmentId, string Title, string Type, int? BestScore, bool? Passed, int AttemptsUsed);

    public record AssignmentGradeDto(Guid AssignmentId, string Title, int? FinalScore, bool? Passed, string AutoGradingStatus, bool ManuallyGraded);
}
