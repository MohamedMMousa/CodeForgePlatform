using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.DeleteAssignment
{
    public class DeleteAssignmentCommandHandler : IRequestHandler<DeleteAssignmentCommand, AssignmentResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteAssignmentCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssignmentResponseDto> Handle(DeleteAssignmentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Assignment was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, assignment.Module.Course, currentUserId);

            _context.Assignments.Remove(assignment);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assignment.deleted", nameof(Assignment), assignment.Id, new { assignment.Title }));

            await _context.SaveChangesAsync(cancellationToken);

            return new AssignmentResponseDto(assignment.Id, "Assignment deleted.");
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
