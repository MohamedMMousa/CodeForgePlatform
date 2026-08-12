namespace CodeForge.Application.MyCourses.Common
{
    public record MyCourseSummaryDto(
        Guid CourseId,
        string Title,
        string? Description,
        string CohortName,
        DateTime CohortStartDate,
        DateTime CohortEndDate);
}
