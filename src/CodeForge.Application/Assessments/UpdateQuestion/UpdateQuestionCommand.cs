using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.UpdateQuestion
{
    public record UpdateQuestionCommand(Guid Id, string QuestionText, List<OptionInputDto> Options)
        : IRequest<QuestionResponseDto>;
}
