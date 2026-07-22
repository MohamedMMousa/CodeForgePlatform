using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.GetAttemptResult
{
    public class GetAttemptResultQueryHandler : IRequestHandler<GetAttemptResultQuery, AttemptResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAttemptResultQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AttemptResultDto> Handle(GetAttemptResultQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var attempt = await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.Quiz).ThenInclude(q => q.Questions).ThenInclude(qq => qq.Options)
                .Include(a => a.Quiz).ThenInclude(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken);

            if (attempt is null)
            {
                throw new KeyNotFoundException("Attempt was not found.");
            }

            if (attempt.StudentId != currentUserId)
            {
                CourseContentAuthorization.EnsureCanManage(_currentUserService, attempt.Quiz.Module.Course, currentUserId);
            }

            if (attempt.SubmittedAt is null)
            {
                throw new InvalidOperationException("This attempt has not been submitted yet.");
            }

            return AttemptResultMapping.ToDto(attempt);
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
