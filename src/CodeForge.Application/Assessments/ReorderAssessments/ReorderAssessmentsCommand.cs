using MediatR;

namespace CodeForge.Application.Assessments.ReorderAssessments
{
    public record AssessmentOrderDto(Guid AssessmentId, int OrderIndex);

    public record ReorderAssessmentsCommand(Guid ModuleId, List<AssessmentOrderDto> AssessmentOrders) : IRequest;
}
