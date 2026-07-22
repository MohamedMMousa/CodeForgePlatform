using MediatR;

namespace CodeForge.Application.Assignments.ReorderTestCases
{
    public record TestCaseOrderDto(Guid TestCaseId, int OrderIndex);

    public record ReorderTestCasesCommand(Guid AssignmentId, List<TestCaseOrderDto> TestCaseOrders) : IRequest;
}
