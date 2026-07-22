namespace CodeForge.Application.Tracks.Common
{
    public record TrackDetailDto(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        decimal Price,
        string Currency,
        string Status,
        Guid CreatedById,
        string CreatedByName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<TrackCourseDto> Courses);
}
