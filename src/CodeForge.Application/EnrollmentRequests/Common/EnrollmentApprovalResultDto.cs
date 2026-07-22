namespace CodeForge.Application.EnrollmentRequests.Common
{
    public record EnrollmentApprovalResultDto(
        Guid RequestId,
        Guid StudentId,
        IReadOnlyList<Guid> EnrollmentIds,
        bool StudentCreated,
        string Message);
}
