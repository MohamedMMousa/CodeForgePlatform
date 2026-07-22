using CodeForge.Application.Announcements.Common;
using MediatR;

namespace CodeForge.Application.Announcements.GetAnnouncements
{
    public record GetAnnouncementsQuery(Guid? CourseId) : IRequest<IReadOnlyList<AnnouncementDto>>;
}
