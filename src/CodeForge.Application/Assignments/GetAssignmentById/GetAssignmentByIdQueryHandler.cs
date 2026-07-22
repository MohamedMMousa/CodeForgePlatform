using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.GetAssignmentById
{
    public class GetAssignmentByIdQueryHandler : IRequestHandler<GetAssignmentByIdQuery, AssignmentDetailDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAssignmentByIdQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssignmentDetailDto> Handle(GetAssignmentByIdQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(a => a.TestCases)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Assignment was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, assignment.Module.Course, currentUserId);

            return AssignmentMapping.ToDetailDto(assignment);
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
