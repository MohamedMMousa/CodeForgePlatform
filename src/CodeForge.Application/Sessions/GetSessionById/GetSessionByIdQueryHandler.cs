using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Sessions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Sessions.GetSessionById
{
    public class GetSessionByIdQueryHandler : IRequestHandler<GetSessionByIdQuery, SessionDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetSessionByIdQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SessionDto> Handle(GetSessionByIdQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Instructor)
                .Include(s => s.Materials)
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (session is null)
            {
                throw new KeyNotFoundException("Session was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, session.Module.Course, currentUserId);

            return SessionMapping.ToDto(session);
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
