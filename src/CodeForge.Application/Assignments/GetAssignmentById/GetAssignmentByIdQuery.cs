using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.GetAssignmentById
{
    public record GetAssignmentByIdQuery(Guid Id) : IRequest<AssignmentDetailDto>;
}
