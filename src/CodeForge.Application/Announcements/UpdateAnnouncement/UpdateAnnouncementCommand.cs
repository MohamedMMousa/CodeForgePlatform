using CodeForge.Application.Announcements.Common;
using MediatR;

namespace CodeForge.Application.Announcements.UpdateAnnouncement
{
    public record UpdateAnnouncementCommand(Guid Id, string Title, string Body) : IRequest<AnnouncementDto>;
}
