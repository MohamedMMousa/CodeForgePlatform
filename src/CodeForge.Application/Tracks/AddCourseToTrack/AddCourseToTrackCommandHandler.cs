using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.AddCourseToTrack
{
    public class AddCourseToTrackCommandHandler : IRequestHandler<AddCourseToTrackCommand, TrackCourseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AddCourseToTrackCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TrackCourseDto> Handle(AddCourseToTrackCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();

            var track = await _context.Tracks.FirstOrDefaultAsync(x => x.Id == request.TrackId, cancellationToken);
            if (track is null)
            {
                throw new KeyNotFoundException("Track was not found.");
            }

            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken);
            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            var alreadyLinked = await _context.TrackCourses
                .AnyAsync(x => x.TrackId == request.TrackId && x.CourseId == request.CourseId, cancellationToken);
            if (alreadyLinked)
            {
                throw new InvalidOperationException("This course is already part of the track.");
            }

            var trackCourse = new TrackCourse
            {
                TrackId = track.Id,
                CourseId = course.Id,
                Course = course,
                SortOrder = request.SortOrder
            };

            _context.TrackCourses.Add(trackCourse);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "track.course_added", nameof(Track), track.Id,
                new { trackTitle = track.Title, courseId = course.Id, courseTitle = course.Title }));

            await _context.SaveChangesAsync(cancellationToken);

            return TrackMapping.ToTrackCourseDto(trackCourse);
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
