using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.GetPublishedTracks
{
    public record GetPublishedTracksQuery(
        string? Search,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<TrackListDto>>;
}
