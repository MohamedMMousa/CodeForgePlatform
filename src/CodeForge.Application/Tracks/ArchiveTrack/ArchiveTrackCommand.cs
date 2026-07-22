using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.ArchiveTrack
{
    public record ArchiveTrackCommand(Guid Id) : IRequest<TrackMutationResultDto>;
}
