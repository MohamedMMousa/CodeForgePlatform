using CodeForge.Application.MyCourses.Common;
using MediatR;

namespace CodeForge.Application.MyCourses.GetUpcomingItems
{
    public record GetUpcomingItemsQuery : IRequest<UpcomingItemsDto>;
}
