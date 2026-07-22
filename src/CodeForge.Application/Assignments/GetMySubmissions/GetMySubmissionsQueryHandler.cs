using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.GetMySubmissions
{
    public class GetMySubmissionsQueryHandler : IRequestHandler<GetMySubmissionsQuery, IReadOnlyList<SubmissionSummaryDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMySubmissionsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<SubmissionSummaryDto>> Handle(GetMySubmissionsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Assignment was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, assignment.Module.Course, currentUserId);

            var submissions = await _context.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => s.AssignmentId == request.AssignmentId && s.StudentId == currentUserId)
                .OrderByDescending(s => s.AttemptNumber)
                .ToListAsync(cancellationToken);

            return submissions
                .Select(s => new SubmissionSummaryDto(
                    s.Id, s.AttemptNumber, s.SubmittedAt, s.IsLate, s.AutoScore, s.AutoGradingStatus, s.ManualScore, s.FinalScore))
                .ToList();
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
