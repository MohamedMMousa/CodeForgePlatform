using CodeForge.Application.Announcements.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using MediatR;

namespace CodeForge.Application.Announcements.GetAnnouncements
{
    public record GetAnnouncementsQuery(
        Guid? CourseId,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<AnnouncementDto>>;
}
