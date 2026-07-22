namespace CodeForge.Application.Enrollments.Common
{
    public record EnrollmentDto(
        Guid Id,
        Guid StudentId,
        string StudentName,
        string StudentEmail,
        Guid CourseId,
        string CourseTitle,
        Guid CohortId,
        string CohortName,
        string Status,
        DateTime? AccessExpiresAt,
        DateTime? CancelledAt,
        string? CancellationReason,
        DateTime CreatedAt);
}
