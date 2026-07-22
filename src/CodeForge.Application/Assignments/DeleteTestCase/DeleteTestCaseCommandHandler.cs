using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.DeleteTestCase
{
    public class DeleteTestCaseCommandHandler : IRequestHandler<DeleteTestCaseCommand, TestCaseResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteTestCaseCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TestCaseResponseDto> Handle(DeleteTestCaseCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var testCase = await _context.AssignmentTestCases
                .Include(tc => tc.Assignment).ThenInclude(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(tc => tc.Id == request.Id, cancellationToken);

            if (testCase is null)
            {
                throw new KeyNotFoundException("Test case was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, testCase.Assignment.Module.Course, currentUserId);

            _context.AssignmentTestCases.Remove(testCase);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assignment_test_case.deleted", nameof(AssignmentTestCase), testCase.Id,
                new { assignmentId = testCase.AssignmentId }));

            await _context.SaveChangesAsync(cancellationToken);

            return new TestCaseResponseDto(testCase.Id, "Test case deleted.");
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
