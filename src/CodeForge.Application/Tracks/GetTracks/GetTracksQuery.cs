using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.GetTracks
{
    public record GetTracksQuery(string? Status, string? Search) : IRequest<IReadOnlyList<TrackListDto>>;
}
