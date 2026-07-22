using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.StartAttempt
{
    public class StartAttemptCommandHandler : IRequestHandler<StartAttemptCommand, StartAttemptResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public StartAttemptCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<StartAttemptResponseDto> Handle(StartAttemptCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var quiz = await _context.Quizzes
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(q => q.Id == request.AssessmentId, cancellationToken);

            if (quiz is null)
            {
                throw new KeyNotFoundException("Assessment was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, quiz.Module.Course, currentUserId);

            if (_currentUserService.Role != Roles.Student)
            {
                throw new InvalidOperationException("Only students can attempt an assessment.");
            }

            var attemptsUsed = await _context.QuizAttempts
                .CountAsync(a => a.QuizId == quiz.Id && a.StudentId == currentUserId, cancellationToken);

            if (quiz.MaxAttempts.HasValue && attemptsUsed >= quiz.MaxAttempts.Value)
            {
                throw new InvalidOperationException("Maximum attempts reached for this assessment.");
            }

            var hasInProgressAttempt = await _context.QuizAttempts
                .AnyAsync(a => a.QuizId == quiz.Id && a.StudentId == currentUserId && a.SubmittedAt == null, cancellationToken);

            if (hasInProgressAttempt)
            {
                throw new InvalidOperationException("An attempt is already in progress for this assessment.");
            }

            var attempt = new QuizAttempt
            {
                QuizId = quiz.Id,
                StudentId = currentUserId,
                AttemptNumber = attemptsUsed + 1,
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync(cancellationToken);

            return new StartAttemptResponseDto(attempt.Id, attempt.StartedAt);
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
