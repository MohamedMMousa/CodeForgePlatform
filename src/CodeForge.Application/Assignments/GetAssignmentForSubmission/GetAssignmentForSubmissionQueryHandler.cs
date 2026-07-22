using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.GetAssignmentForSubmission
{
    public class GetAssignmentForSubmissionQueryHandler : IRequestHandler<GetAssignmentForSubmissionQuery, AssignmentForSubmissionDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAssignmentForSubmissionQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssignmentForSubmissionDto> Handle(GetAssignmentForSubmissionQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .Include(a => a.TestCases)
                .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Assignment was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, assignment.Module.Course, currentUserId);

            var attemptsUsed = await _context.AssignmentSubmissions
                .CountAsync(s => s.AssignmentId == assignment.Id && s.StudentId == currentUserId, cancellationToken);

            var sampleTestCases = assignment.TestCases
                .Where(tc => !tc.IsHidden)
                .OrderBy(tc => tc.OrderIndex)
                .Select(tc => new SubmissionTestCaseDto(tc.Id, tc.Input, tc.ExpectedOutput))
                .ToList();

            return new AssignmentForSubmissionDto(
                assignment.Id, assignment.Title, assignment.Description, assignment.DueAt,
                assignment.MaxAttempts, attemptsUsed, sampleTestCases);
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
