using CodeForge.Application.Announcements.Common;

namespace CodeForge.Application.MyCourses.Common
{
    public record UpcomingItemsDto(
        IReadOnlyList<UpcomingSessionDto> UpcomingSessions,
        IReadOnlyList<AnnouncementDto> RecentAnnouncements);
}
