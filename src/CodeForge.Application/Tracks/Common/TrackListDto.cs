namespace CodeForge.Application.Tracks.Common
{
    public record TrackListDto(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        decimal Price,
        string Currency,
        string Status,
        int CourseCount,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
