using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.PublishTrack
{
    public class PublishTrackCommandHandler : IRequestHandler<PublishTrackCommand, TrackMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public PublishTrackCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TrackMutationResultDto> Handle(PublishTrackCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var track = await _context.Tracks
                .Include(x => x.TrackCourses)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (track is null)
            {
                throw new KeyNotFoundException("Track was not found.");
            }

            if (track.Status == TrackStatuses.Published)
            {
                return new TrackMutationResultDto(track.Id, "Track is already published.");
            }

            if (track.TrackCourses.Count == 0)
            {
                throw new InvalidOperationException("Add at least one course to the track before publishing.");
            }

            track.Status = TrackStatuses.Published;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "track.published", nameof(Track), track.Id, new { track.Title, track.Slug }));

            await _context.SaveChangesAsync(cancellationToken);

            return new TrackMutationResultDto(track.Id, "Track published.");
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
