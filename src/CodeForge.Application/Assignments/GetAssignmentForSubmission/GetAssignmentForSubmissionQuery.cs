using MediatR;

namespace CodeForge.Application.Assignments.GetAssignmentForSubmission
{
    public record SubmissionTestCaseDto(Guid Id, string Input, string ExpectedOutput);

    public record AssignmentForSubmissionDto(
        Guid Id,
        string Title,
        string Description,
        DateTime? DueAt,
        int? MaxAttempts,
        int AttemptsUsed,
        List<SubmissionTestCaseDto> SampleTestCases);

    public record GetAssignmentForSubmissionQuery(Guid AssignmentId) : IRequest<AssignmentForSubmissionDto>;
}
