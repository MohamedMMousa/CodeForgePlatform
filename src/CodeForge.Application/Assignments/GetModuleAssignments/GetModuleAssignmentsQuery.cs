using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.GetModuleAssignments
{
    public record GetModuleAssignmentsQuery(Guid ModuleId) : IRequest<IReadOnlyList<AssignmentDto>>;
}
