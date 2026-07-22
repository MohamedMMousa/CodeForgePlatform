using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.CreateTrack
{
    public class CreateTrackCommandHandler : IRequestHandler<CreateTrackCommand, TrackDetailDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateTrackCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TrackDetailDto> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var slug = request.Slug.Trim().ToLower();

            var slugExists = await _context.Tracks.AnyAsync(x => x.Slug == slug, cancellationToken);
            if (slugExists)
            {
                throw new InvalidOperationException("Track slug is already in use.");
            }

            var track = new Track
            {
                Title = request.Title.Trim(),
                Slug = slug,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim(),
                Price = request.Price,
                Currency = request.Currency.Trim().ToUpper(),
                Status = TrackStatuses.Draft,
                CreatedById = adminId
            };

            _context.Tracks.Add(track);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "track.created", nameof(Track), track.Id,
                new { track.Title, track.Slug, track.Price, track.Currency }));

            await _context.SaveChangesAsync(cancellationToken);

            track.CreatedBy = await _context.Users.AsNoTracking().FirstAsync(x => x.Id == adminId, cancellationToken);

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
