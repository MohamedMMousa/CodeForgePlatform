using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.CreateAssignment
{
    public record CreateAssignmentCommand(
        Guid ModuleId,
        string Title,
        string Description,
        bool IsPractice,
        int? MaxAttempts,
        DateTime? DueAt,
        int? PassScore) : IRequest<AssignmentResponseDto>;
}
