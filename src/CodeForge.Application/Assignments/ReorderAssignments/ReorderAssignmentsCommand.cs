using MediatR;

namespace CodeForge.Application.Assignments.ReorderAssignments
{
    public record AssignmentOrderDto(Guid AssignmentId, int OrderIndex);

    public record ReorderAssignmentsCommand(Guid ModuleId, List<AssignmentOrderDto> AssignmentOrders) : IRequest;
}
