namespace CodeForge.Application.Assignments.Common
{
    public record AssignmentResponseDto(Guid AssignmentId, string Message);

    public record TestCaseResponseDto(Guid TestCaseId, string Message);
}
