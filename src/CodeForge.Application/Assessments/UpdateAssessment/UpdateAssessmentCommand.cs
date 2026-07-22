using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.UpdateAssessment
{
    public record UpdateAssessmentCommand(
        Guid Id,
        string Type,
        string Title,
        int? TimeLimitMinutes,
        int? PassScore,
        bool IsPractice,
        int? MaxAttempts,
        bool RandomizeQuestions,
        bool DisableCopyPaste) : IRequest<AssessmentResponseDto>;
}
