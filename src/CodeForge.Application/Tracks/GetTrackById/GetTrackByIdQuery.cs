using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.GetTrackById
{
    public record GetTrackByIdQuery(Guid Id) : IRequest<TrackDetailDto>;
}
