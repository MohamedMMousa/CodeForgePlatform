using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.DeleteAssignment
{
    public record DeleteAssignmentCommand(Guid Id) : IRequest<AssignmentResponseDto>;
}
