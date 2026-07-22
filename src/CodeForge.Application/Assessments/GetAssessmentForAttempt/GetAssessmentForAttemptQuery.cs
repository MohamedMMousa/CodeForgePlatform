using MediatR;

namespace CodeForge.Application.Assessments.GetAssessmentForAttempt
{
    public record AttemptOptionDto(Guid Id, string OptionText);

    public record AttemptQuestionDto(Guid Id, string QuestionText, List<AttemptOptionDto> Options);

    public record AttemptAssessmentDto(
        Guid Id,
        string Type,
        string Title,
        int? TimeLimitMinutes,
        int? MaxAttempts,
        int AttemptsUsed,
        bool DisableCopyPaste,
        List<AttemptQuestionDto> Questions);

    public record GetAssessmentForAttemptQuery(Guid AssessmentId) : IRequest<AttemptAssessmentDto>;
}
