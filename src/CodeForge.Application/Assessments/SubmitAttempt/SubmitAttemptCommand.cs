using CodeForge.Application.Assessments.Common;
using MediatR;

namespace CodeForge.Application.Assessments.SubmitAttempt
{
    public record AnswerInputDto(Guid QuestionId, Guid? SelectedOptionId);

    public record SubmitAttemptCommand(Guid AttemptId, List<AnswerInputDto> Answers) : IRequest<AttemptResultDto>;
}
