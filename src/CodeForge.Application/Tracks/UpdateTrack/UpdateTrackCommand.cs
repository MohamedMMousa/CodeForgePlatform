using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.UpdateTrack
{
    public record UpdateTrackCommand(
        Guid Id,
        string Title,
        string Slug,
        string? Description,
        string? ThumbnailUrl,
        decimal Price,
        string Currency) : IRequest<TrackDetailDto>;
}
