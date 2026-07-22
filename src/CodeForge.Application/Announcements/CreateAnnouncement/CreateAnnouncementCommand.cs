using CodeForge.Application.Announcements.Common;
using MediatR;

namespace CodeForge.Application.Announcements.CreateAnnouncement
{
    public record CreateAnnouncementCommand(Guid? CourseId, string Title, string Body) : IRequest<AnnouncementDto>;
}
