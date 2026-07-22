using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.GetModuleAssignments
{
    public class GetModuleAssignmentsQueryHandler : IRequestHandler<GetModuleAssignmentsQuery, IReadOnlyList<AssignmentDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetModuleAssignmentsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<AssignmentDto>> Handle(GetModuleAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            // Manage view exposes ExpectedOutput on test cases — instructor/admin only.
            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            var assignments = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.TestCases)
                .Where(a => a.ModuleId == request.ModuleId)
                .OrderBy(a => a.OrderIndex)
                .ToListAsync(cancellationToken);

            return assignments.Select(AssignmentMapping.ToDto).ToList();
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
