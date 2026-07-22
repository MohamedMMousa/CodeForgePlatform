using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.GetSubmissionsForGrading
{
    public class GetSubmissionsForGradingQueryHandler : IRequestHandler<GetSubmissionsForGradingQuery, AssignmentSubmissionsDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetSubmissionsForGradingQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssignmentSubmissionsDto> Handle(GetSubmissionsForGradingQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Assignment was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, assignment.Module.Course, currentUserId);

            var submissions = await _context.AssignmentSubmissions
                .AsNoTracking()
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == request.AssignmentId)
                .OrderBy(s => s.Student.FullName).ThenByDescending(s => s.AttemptNumber)
                .ToListAsync(cancellationToken);

            var submissionDtos = submissions
                .Select(s => new StudentSubmissionDto(
                    s.Id, s.StudentId, s.Student.FullName, s.AttemptNumber, s.SubmittedAt, s.IsLate,
                    s.AutoScore, s.AutoGradingStatus, s.ManualScore, s.FinalScore))
                .ToList();

            return new AssignmentSubmissionsDto(assignment.Id, assignment.Title, submissionDtos);
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
