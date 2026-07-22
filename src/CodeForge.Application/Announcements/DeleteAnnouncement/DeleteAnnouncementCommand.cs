using CodeForge.Application.Announcements.Common;
using MediatR;

namespace CodeForge.Application.Announcements.DeleteAnnouncement
{
    public record DeleteAnnouncementCommand(Guid Id) : IRequest<AnnouncementDto>;
}
