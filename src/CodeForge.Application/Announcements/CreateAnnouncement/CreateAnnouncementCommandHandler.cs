using CodeForge.Application.Announcements.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Announcements.CreateAnnouncement
{
    public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, AnnouncementDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateAnnouncementCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AnnouncementDto> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            Course? course = null;

            if (request.CourseId.HasValue)
            {
                course = await _context.Courses
                    .Include(c => c.Instructors)
                    .FirstOrDefaultAsync(c => c.Id == request.CourseId.Value, cancellationToken);

                if (course is null)
                {
                    throw new KeyNotFoundException("Course was not found.");
                }

                CourseContentAuthorization.EnsureCanManage(_currentUserService, course, currentUserId);
            }
            else if (_currentUserService.Role != Roles.Admin)
            {
                throw new UnauthorizedAccessException("Only an admin can post a platform-wide announcement.");
            }

            var announcement = new Announcement
            {
                CourseId = request.CourseId,
                Course = course,
                AuthorId = currentUserId,
                Title = request.Title.Trim(),
                Body = request.Body.Trim()
            };

            _context.Announcements.Add(announcement);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "announcement.created", nameof(Announcement), announcement.Id,
                new { announcement.Title, courseId = request.CourseId }));

            await _context.SaveChangesAsync(cancellationToken);

            announcement.Author = await _context.Users.AsNoTracking()
                .FirstAsync(u => u.Id == currentUserId, cancellationToken);

            return AnnouncementMapping.ToDto(announcement);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated user could not be resolved.");
            }

            return userId;
        }
    }
}
