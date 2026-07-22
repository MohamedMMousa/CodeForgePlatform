using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.UpdateAssignment
{
    public record UpdateAssignmentCommand(
        Guid Id,
        string Title,
        string Description,
        bool IsPractice,
        int? MaxAttempts,
        DateTime? DueAt,
        int? PassScore) : IRequest<AssignmentResponseDto>;
}
