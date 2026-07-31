using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Tracks.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.GetTracks
{
    public class GetTracksQueryHandler : IRequestHandler<GetTracksQuery, PagedResult<TrackListDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetTracksQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TrackListDto>> Handle(GetTracksQuery request, CancellationToken cancellationToken)
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

            var totalCount = await query.CountAsync(cancellationToken);

            var tracks = await query
                .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = tracks.Select(TrackMapping.ToListDto).ToList();

            return new PagedResult<TrackListDto>(items, request.Page, request.PageSize, totalCount);
        }
    }
}
