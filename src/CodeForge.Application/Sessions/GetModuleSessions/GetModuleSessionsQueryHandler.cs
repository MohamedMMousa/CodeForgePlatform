using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Sessions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Sessions.GetModuleSessions
{
    public class GetModuleSessionsQueryHandler : IRequestHandler<GetModuleSessionsQuery, IReadOnlyList<SessionDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetModuleSessionsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<SessionDto>> Handle(
            GetModuleSessionsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(m => m.Course).ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, module.Course, currentUserId);

            var sessions = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Instructor)
                .Include(s => s.Materials)
                .Where(s => s.ModuleId == request.ModuleId)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync(cancellationToken);

            return sessions.Select(SessionMapping.ToDto).ToList();
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
