using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.UpdateTrack
{
    public class UpdateTrackCommandHandler : IRequestHandler<UpdateTrackCommand, TrackDetailDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateTrackCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TrackDetailDto> Handle(UpdateTrackCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var slug = request.Slug.Trim().ToLower();

            var track = await _context.Tracks
                .Include(x => x.CreatedBy)
                .Include(x => x.TrackCourses).ThenInclude(tc => tc.Course)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (track is null)
            {
                throw new KeyNotFoundException("Track was not found.");
            }

            var slugExists = await _context.Tracks
                .AnyAsync(x => x.Id != request.Id && x.Slug == slug, cancellationToken);
            if (slugExists)
            {
                throw new InvalidOperationException("Track slug is already in use.");
            }

            track.Title = request.Title.Trim();
            track.Slug = slug;
            track.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            track.ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim();
            track.Price = request.Price;
            track.Currency = request.Currency.Trim().ToUpper();

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "track.updated", nameof(Track), track.Id,
                new { track.Title, track.Slug, track.Price, track.Currency }));

            await _context.SaveChangesAsync(cancellationToken);

            return TrackMapping.ToDetailDto(track);
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
