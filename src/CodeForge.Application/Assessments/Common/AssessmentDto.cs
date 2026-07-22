namespace CodeForge.Application.Assessments.Common
{
    public record AssessmentDto(
        Guid Id,
        Guid ModuleId,
        string Type,
        string Title,
        int OrderIndex,
        int? TimeLimitMinutes,
        int? PassScore,
        bool IsPractice,
        int? MaxAttempts,
        bool RandomizeQuestions,
        bool DisableCopyPaste,
        int QuestionCount,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public record AssessmentDetailDto(
        Guid Id,
        Guid ModuleId,
        string Type,
        string Title,
        int OrderIndex,
        int? TimeLimitMinutes,
        int? PassScore,
        bool IsPractice,
        int? MaxAttempts,
        bool RandomizeQuestions,
        bool DisableCopyPaste,
        List<QuestionDto> Questions);
}
