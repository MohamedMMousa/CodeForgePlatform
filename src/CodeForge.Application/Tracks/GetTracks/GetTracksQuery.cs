using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.GetTracks
{
    public record GetTracksQuery(
        string? Status,
        string? Search,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<TrackListDto>>;
}
