namespace CodeForge.Application.Courses.Common
{
    public record CourseDetailDto(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        string? Category,
        decimal Price,
        string Currency,
        string Status,
        decimal? CompletionAttendanceThreshold,
        Guid CreatedById,
        string CreatedByName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<CourseInstructorDto> Instructors);
}
