using CodeForge.Domain.Entities;

namespace CodeForge.Application.Tracks.Common
{
    public static class TrackMapping
    {
        public static TrackListDto ToListDto(Track track)
        {
            return new TrackListDto(
                track.Id,
                track.Title,
                track.Slug,
                track.Description,
                track.ThumbnailUrl,
                track.Price,
                track.Currency,
                track.Status,
                track.TrackCourses.Count,
                track.CreatedAt,
                track.UpdatedAt);
        }

        public static TrackDetailDto ToDetailDto(Track track)
        {
            return new TrackDetailDto(
                track.Id,
                track.Title,
                track.Slug,
                track.Description,
                track.ThumbnailUrl,
                track.Price,
                track.Currency,
                track.Status,
                track.CreatedById,
                track.CreatedBy.FullName,
                track.CreatedAt,
                track.UpdatedAt,
                track.TrackCourses
                    .OrderBy(x => x.SortOrder)
                    .Select(ToTrackCourseDto)
                    .ToList());
        }

        public static TrackCourseDto ToTrackCourseDto(TrackCourse trackCourse)
        {
            return new TrackCourseDto(
                trackCourse.CourseId,
                trackCourse.Course.Title,
                trackCourse.Course.Slug,
                trackCourse.Course.Price,
                trackCourse.SortOrder);
        }
    }
}
