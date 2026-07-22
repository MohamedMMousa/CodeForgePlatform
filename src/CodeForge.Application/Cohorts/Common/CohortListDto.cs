namespace CodeForge.Application.Cohorts.Common
{
    public record CohortListDto(
        Guid Id,
        Guid CourseId,
        string CourseTitle,
        string Name,
        DateTime StartDate,
        DateTime EndDate,
        DateTime EnrollmentCutoffDate,
        int Capacity,
        int GracePeriodDays,
        string Status,
        int EnrolledCount,
        int SeatsLeft,
        bool IsAcceptingEnrollment,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
