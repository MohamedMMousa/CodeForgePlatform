using MediatR;

namespace CodeForge.Application.Assessments.ReorderQuestions
{
    public record QuestionOrderDto(Guid QuestionId, int OrderIndex);

    public record ReorderQuestionsCommand(Guid AssessmentId, List<QuestionOrderDto> QuestionOrders) : IRequest;
}
