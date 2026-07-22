namespace CodeForge.Application.Assessments.Common
{
    public record AnswerResultDto(
        Guid QuestionId,
        string QuestionText,
        Guid? SelectedOptionId,
        bool? IsCorrectSelection,
        List<OptionDto> Options);

    public record AttemptResultDto(
        Guid AttemptId,
        Guid QuizId,
        string QuizTitle,
        int AttemptNumber,
        int? Score,
        bool? Passed,
        DateTime StartedAt,
        DateTime? SubmittedAt,
        List<AnswerResultDto> Answers);
}
