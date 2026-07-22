namespace CodeForge.Application.Announcements.Common
{
    public record AnnouncementDto(
        Guid Id,
        Guid? CourseId,
        string? CourseTitle,
        Guid AuthorId,
        string AuthorName,
        string Title,
        string Body,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
