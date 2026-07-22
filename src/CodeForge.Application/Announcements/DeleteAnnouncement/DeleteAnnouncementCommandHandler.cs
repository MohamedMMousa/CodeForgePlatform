using CodeForge.Application.Announcements.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Announcements.DeleteAnnouncement
{
    public class DeleteAnnouncementCommandHandler : IRequestHandler<DeleteAnnouncementCommand, AnnouncementDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteAnnouncementCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AnnouncementDto> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var announcement = await _context.Announcements
                .Include(a => a.Course)
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (announcement is null)
            {
                throw new KeyNotFoundException("Announcement was not found.");
            }

            if (_currentUserService.Role != Roles.Admin && announcement.AuthorId != currentUserId)
            {
                throw new UnauthorizedAccessException("Only the author or an admin can delete this announcement.");
            }

            _context.Announcements.Remove(announcement);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "announcement.deleted", nameof(Announcement), announcement.Id,
                new { announcement.Title }));

            await _context.SaveChangesAsync(cancellationToken);

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
