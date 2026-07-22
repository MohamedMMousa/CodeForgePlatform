using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.CreateTrack
{
    public record CreateTrackCommand(
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        decimal Price,
        string Currency) : IRequest<TrackDetailDto>;
}
