using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Modules.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Modules.GetCourseModules
{
    public class GetCourseModulesQueryHandler : IRequestHandler<GetCourseModulesQuery, IReadOnlyList<ModuleListDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCourseModulesQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<ModuleListDto>> Handle(
            GetCourseModulesQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Instructors)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, course, currentUserId);

            var modules = await _context.Modules
                .AsNoTracking()
                .Include(m => m.Sessions)
                .Where(m => m.CourseId == request.CourseId)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync(cancellationToken);

            return modules.Select(ModuleMapping.ToDto).ToList();
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
