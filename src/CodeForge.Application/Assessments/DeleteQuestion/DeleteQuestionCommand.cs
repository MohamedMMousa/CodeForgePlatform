using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.DeleteQuestion
{
    public record DeleteQuestionCommand(Guid Id) : IRequest<QuestionResponseDto>;
}
