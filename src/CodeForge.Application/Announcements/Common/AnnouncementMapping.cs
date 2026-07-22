using CodeForge.Domain.Entities;

namespace CodeForge.Application.Announcements.Common
{
    public static class AnnouncementMapping
    {
        public static AnnouncementDto ToDto(Announcement announcement)
        {
            return new AnnouncementDto(
                announcement.Id,
                announcement.CourseId,
                announcement.Course?.Title,
                announcement.AuthorId,
                announcement.Author.FullName,
                announcement.Title,
                announcement.Body,
                announcement.CreatedAt,
                announcement.UpdatedAt);
        }
    }
}
