using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.ReorderAssignments
{
    public class ReorderAssignmentsCommandHandler : IRequestHandler<ReorderAssignmentsCommand>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReorderAssignmentsCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(ReorderAssignmentsCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(m => m.Assignments)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            var assignmentIds = request.AssignmentOrders.Select(x => x.AssignmentId).ToList();
            var invalidAssignments = assignmentIds.Except(module.Assignments.Select(a => a.Id)).ToList();
            if (invalidAssignments.Count != 0)
            {
                throw new InvalidOperationException("One or more assignments do not belong to the specified module.");
            }

            var duplicateOrders = request.AssignmentOrders.GroupBy(x => x.OrderIndex).Where(g => g.Count() > 1).ToList();
            if (duplicateOrders.Count != 0)
            {
                throw new InvalidOperationException("Duplicate order indices are not allowed.");
            }

            foreach (var assignmentOrder in request.AssignmentOrders)
            {
                var assignment = module.Assignments.First(a => a.Id == assignmentOrder.AssignmentId);
                assignment.OrderIndex = assignmentOrder.OrderIndex;
            }

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assignments.reordered", nameof(Module), module.Id, new { moduleId = module.Id }));

            await _context.SaveChangesAsync(cancellationToken);
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
