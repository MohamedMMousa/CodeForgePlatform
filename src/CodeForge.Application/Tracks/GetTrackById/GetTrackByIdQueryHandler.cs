using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Tracks.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Tracks.GetTrackById
{
    public class GetTrackByIdQueryHandler : IRequestHandler<GetTrackByIdQuery, TrackDetailDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetTrackByIdQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<TrackDetailDto> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var track = await _context.Tracks
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Include(x => x.TrackCourses).ThenInclude(tc => tc.Course)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (track is null)
            {
                throw new KeyNotFoundException("Track was not found.");
            }

            return TrackMapping.ToDetailDto(track);
        }
    }
}
