using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.GetAssessmentResults
{
    public class GetAssessmentResultsQueryHandler : IRequestHandler<GetAssessmentResultsQuery, AssessmentResultsDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAssessmentResultsQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssessmentResultsDto> Handle(GetAssessmentResultsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(q => q.Id == request.AssessmentId, cancellationToken);

            if (quiz is null)
            {
                throw new KeyNotFoundException("Assessment was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, quiz.Module.Course, currentUserId);

            var attempts = await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.Student)
                .Where(a => a.QuizId == request.AssessmentId)
                .OrderBy(a => a.Student.FullName).ThenByDescending(a => a.AttemptNumber)
                .ToListAsync(cancellationToken);

            var attemptDtos = attempts
                .Select(a => new StudentAttemptDto(
                    a.Id, a.StudentId, a.Student.FullName, a.AttemptNumber, a.Score, a.Passed, a.StartedAt, a.SubmittedAt))
                .ToList();

            return new AssessmentResultsDto(quiz.Id, quiz.Title, attemptDtos);
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
