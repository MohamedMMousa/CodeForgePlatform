using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.GetPublishedTracks
{
    public record GetPublishedTracksQuery(string? Search) : IRequest<IReadOnlyList<TrackListDto>>;
}
