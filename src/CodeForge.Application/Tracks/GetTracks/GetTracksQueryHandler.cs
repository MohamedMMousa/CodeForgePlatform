using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.GetTracks
{
    public class GetTracksQueryHandler : IRequestHandler<GetTracksQuery, IReadOnlyList<TrackListDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetTracksQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<TrackListDto>> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Tracks
                .AsNoTracking()
                .Include(x => x.TrackCourses)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim().ToLower();
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(search) ||
                    x.Slug.ToLower().Contains(search));
            }

            var tracks = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return tracks.Select(TrackMapping.ToListDto).ToList();
        }
    }
}
