using CodeForge.Application.Cohorts.Common;

namespace CodeForge.Application.Courses.Common
{
    /// <summary>
    /// Public catalog detail view — adds open-batch availability on top of the base
    /// course fields, which the plain admin CourseDetailDto doesn't need.
    /// </summary>
    public record PublicCourseDetailDto(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        string? Category,
        decimal Price,
        string Currency,
        IReadOnlyList<CourseInstructorDto> Instructors,
        IReadOnlyList<CohortListDto> Cohorts);
}
