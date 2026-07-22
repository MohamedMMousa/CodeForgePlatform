using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.CreateAssessment
{
    public record CreateAssessmentCommand(
        Guid ModuleId,
        string Type,
        string Title,
        int? TimeLimitMinutes,
        int? PassScore,
        bool IsPractice,
        int? MaxAttempts,
        bool RandomizeQuestions,
        bool DisableCopyPaste) : IRequest<AssessmentResponseDto>;
}
