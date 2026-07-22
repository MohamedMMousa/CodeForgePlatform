namespace CodeForge.Application.Courses.Common
{
    public record CourseListDto(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        string? Category,
        decimal Price,
        string Currency,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
