using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.RemoveCourseFromTrack
{
    public class RemoveCourseFromTrackCommandHandler
        : IRequestHandler<RemoveCourseFromTrackCommand, TrackMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public RemoveCourseFromTrackCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TrackMutationResultDto> Handle(
            RemoveCourseFromTrackCommand request,
            CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var trackCourse = await _context.TrackCourses
                .Include(x => x.Track)
                .Include(x => x.Course)
                .FirstOrDefaultAsync(
                    x => x.TrackId == request.TrackId && x.CourseId == request.CourseId,
                    cancellationToken);

            if (trackCourse is null)
            {
                throw new KeyNotFoundException("This course is not part of the track.");
            }

            _context.TrackCourses.Remove(trackCourse);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "track.course_removed", nameof(Track), request.TrackId,
                new { trackTitle = trackCourse.Track.Title, courseId = request.CourseId, courseTitle = trackCourse.Course.Title }));

            await _context.SaveChangesAsync(cancellationToken);

            return new TrackMutationResultDto(request.TrackId, "Course removed from track.");
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return userId;
        }
    }
}
