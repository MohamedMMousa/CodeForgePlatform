namespace CodeForge.Application.Tracks.Common
{
    public record PublicTrackDetailDto(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        decimal Price,
        string Currency,
        IReadOnlyList<TrackCourseDto> Courses,
        bool IsBundleEnrollable);
}
