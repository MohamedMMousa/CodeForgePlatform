using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.AddTestCase
{
    public class AddTestCaseCommandHandler : IRequestHandler<AddTestCaseCommand, TestCaseResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AddTestCaseCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TestCaseResponseDto> Handle(AddTestCaseCommand request, CancellationToken cancellationToken)
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

            var maxOrder = assignment.TestCases.Count == 0 ? 0 : assignment.TestCases.Max(tc => tc.OrderIndex);

            var testCase = new AssignmentTestCase
            {
                AssignmentId = assignment.Id,
                Input = request.Input,
                ExpectedOutput = request.ExpectedOutput,
                IsHidden = request.IsHidden,
                Points = request.Points,
                OrderIndex = maxOrder + 1,
            };

            _context.AssignmentTestCases.Add(testCase);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assignment_test_case.created", nameof(AssignmentTestCase), testCase.Id,
                new { assignmentId = assignment.Id }));

            await _context.SaveChangesAsync(cancellationToken);

            return new TestCaseResponseDto(testCase.Id, "Test case added.");
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
