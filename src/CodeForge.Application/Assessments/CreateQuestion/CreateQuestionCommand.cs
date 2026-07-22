using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.CreateQuestion
{
    public record CreateQuestionCommand(Guid AssessmentId, string QuestionText, List<OptionInputDto> Options)
        : IRequest<QuestionResponseDto>;
}
