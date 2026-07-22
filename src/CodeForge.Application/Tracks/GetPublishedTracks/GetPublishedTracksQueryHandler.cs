using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.GetPublishedTracks
{
    public class GetPublishedTracksQueryHandler
        : IRequestHandler<GetPublishedTracksQuery, IReadOnlyList<TrackListDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetPublishedTracksQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<TrackListDto>> Handle(
            GetPublishedTracksQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Tracks
                .AsNoTracking()
                .Include(x => x.TrackCourses)
                .Where(x => x.Status == TrackStatuses.Published);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(search) ||
                    x.Slug.ToLower().Contains(search));
            }

            var tracks = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            return tracks.Select(TrackMapping.ToListDto).ToList();
        }
    }
}
