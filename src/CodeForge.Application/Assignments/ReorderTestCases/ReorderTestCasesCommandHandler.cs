using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.ReorderTestCases
{
    public class ReorderTestCasesCommandHandler : IRequestHandler<ReorderTestCasesCommand>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReorderTestCasesCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(ReorderTestCasesCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(a => a.TestCases)
                .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Assignment was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, assignment.Module.Course, currentUserId);

            var testCaseIds = request.TestCaseOrders.Select(x => x.TestCaseId).ToList();
            var invalidTestCases = testCaseIds.Except(assignment.TestCases.Select(tc => tc.Id)).ToList();
            if (invalidTestCases.Count != 0)
            {
                throw new InvalidOperationException("One or more test cases do not belong to the specified assignment.");
            }

            var duplicateOrders = request.TestCaseOrders.GroupBy(x => x.OrderIndex).Where(g => g.Count() > 1).ToList();
            if (duplicateOrders.Count != 0)
            {
                throw new InvalidOperationException("Duplicate order indices are not allowed.");
            }

            foreach (var testCaseOrder in request.TestCaseOrders)
            {
                var testCase = assignment.TestCases.First(tc => tc.Id == testCaseOrder.TestCaseId);
                testCase.OrderIndex = testCaseOrder.OrderIndex;
            }

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assignment_test_cases.reordered", nameof(Assignment), assignment.Id,
                new { assignmentId = assignment.Id }));

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
