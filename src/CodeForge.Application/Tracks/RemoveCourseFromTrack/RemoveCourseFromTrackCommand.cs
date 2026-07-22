using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.RemoveCourseFromTrack
{
    public record RemoveCourseFromTrackCommand(Guid TrackId, Guid CourseId) : IRequest<TrackMutationResultDto>;
}
