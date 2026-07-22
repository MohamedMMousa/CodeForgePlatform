namespace CodeForge.Application.Tracks.Common
{
    public record TrackCourseDto(
        Guid CourseId,
        string CourseTitle,
        string CourseSlug,
        decimal CoursePrice,
        int SortOrder);
}
