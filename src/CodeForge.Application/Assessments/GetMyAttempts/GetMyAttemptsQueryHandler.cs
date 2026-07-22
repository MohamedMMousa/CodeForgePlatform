using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.GetMyAttempts
{
    public class GetMyAttemptsQueryHandler : IRequestHandler<GetMyAttemptsQuery, IReadOnlyList<AttemptSummaryDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyAttemptsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IReadOnlyList<AttemptSummaryDto>> Handle(GetMyAttemptsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(q => q.Id == request.AssessmentId, cancellationToken);

            if (quiz is null)
            {
                throw new KeyNotFoundException("Assessment was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, quiz.Module.Course, currentUserId);

            var attempts = await _context.QuizAttempts
                .AsNoTracking()
                .Where(a => a.QuizId == request.AssessmentId && a.StudentId == currentUserId)
                .OrderByDescending(a => a.AttemptNumber)
                .ToListAsync(cancellationToken);

            return attempts
                .Select(a => new AttemptSummaryDto(a.Id, a.AttemptNumber, a.Score, a.Passed, a.StartedAt, a.SubmittedAt))
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
