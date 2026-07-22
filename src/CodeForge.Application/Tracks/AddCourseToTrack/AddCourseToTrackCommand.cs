using CodeForge.Application.Tracks.Common;
using MediatR;

namespace CodeForge.Application.Tracks.AddCourseToTrack
{
    public record AddCourseToTrackCommand(Guid TrackId, Guid CourseId, int SortOrder) : IRequest<TrackCourseDto>;
}
