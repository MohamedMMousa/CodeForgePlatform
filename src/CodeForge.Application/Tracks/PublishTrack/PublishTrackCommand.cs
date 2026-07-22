using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.PublishTrack
{
    public record PublishTrackCommand(Guid Id) : IRequest<TrackMutationResultDto>;
}
