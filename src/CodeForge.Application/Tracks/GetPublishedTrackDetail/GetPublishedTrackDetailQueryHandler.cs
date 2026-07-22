using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.GetPublishedTrackDetail
{
    public class GetPublishedTrackDetailQueryHandler
        : IRequestHandler<GetPublishedTrackDetailQuery, PublicTrackDetailDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetPublishedTrackDetailQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PublicTrackDetailDto> Handle(
            GetPublishedTrackDetailQuery request,
            CancellationToken cancellationToken)
        {
            var slug = request.Slug.Trim().ToLower();
            var track = await _context.Tracks
                .AsNoTracking()
                .Include(x => x.TrackCourses).ThenInclude(tc => tc.Course)
                .FirstOrDefaultAsync(
                    x => x.Slug == slug && x.Status == TrackStatuses.Published,
                    cancellationToken);

            if (track is null)
            {
                throw new KeyNotFoundException("Published track was not found.");
            }

            var now = DateTime.UtcNow;
            var isBundleEnrollable = true;
            foreach (var trackCourse in track.TrackCourses)
            {
                var cohort = await CohortAvailability.FindOpenCohortAsync(
                    _context, trackCourse.CourseId, now, cancellationToken);
                if (cohort is null)
                {
                    isBundleEnrollable = false;
                    break;
                }
            }

            return new PublicTrackDetailDto(
                track.Id,
                track.Title,
                track.Slug,
                track.Description,
                track.ThumbnailUrl,
                track.Price,
                track.Currency,
                track.TrackCourses
                    .OrderBy(x => x.SortOrder)
                    .Select(TrackMapping.ToTrackCourseDto)
                    .ToList(),
                isBundleEnrollable);
        }
    }
}
