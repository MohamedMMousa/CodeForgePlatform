using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.ArchiveTrack
{
    public class ArchiveTrackCommandHandler : IRequestHandler<ArchiveTrackCommand, TrackMutationResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ArchiveTrackCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TrackMutationResultDto> Handle(ArchiveTrackCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var track = await _context.Tracks.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (track is null)
            {
                throw new KeyNotFoundException("Track was not found.");
            }

            track.Status = TrackStatuses.Archived;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "track.archived", nameof(Track), track.Id, new { track.Title, track.Slug }));

            await _context.SaveChangesAsync(cancellationToken);

            return new TrackMutationResultDto(track.Id, "Track archived.");
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
