using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.DeleteTrack
{
    public record DeleteTrackCommand(Guid Id) : IRequest<TrackMutationResultDto>;
}
