using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.SubmitAttempt
{
    public class SubmitAttemptCommandHandler : IRequestHandler<SubmitAttemptCommand, AttemptResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SubmitAttemptCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AttemptResultDto> Handle(SubmitAttemptCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz).ThenInclude(q => q.Questions).ThenInclude(qq => qq.Options)
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken);

            if (attempt is null)
            {
                throw new KeyNotFoundException("Attempt was not found.");
            }

            if (attempt.StudentId != currentUserId)
            {
                throw new UnauthorizedAccessException("This attempt does not belong to the current user.");
            }

            if (attempt.SubmittedAt != null)
            {
                throw new InvalidOperationException("This attempt has already been submitted.");
            }

            var questionIds = attempt.Quiz.Questions.Select(q => q.Id).ToHashSet();
            var invalidQuestions = request.Answers.Select(a => a.QuestionId).Except(questionIds).ToList();
            if (invalidQuestions.Count != 0)
            {
                throw new InvalidOperationException("One or more answers reference questions outside this assessment.");
            }

            var correctCount = 0;
            foreach (var answerInput in request.Answers)
            {
                var question = attempt.Quiz.Questions.First(q => q.Id == answerInput.QuestionId);
                var isCorrect = answerInput.SelectedOptionId.HasValue
                    && question.Options.Any(o => o.Id == answerInput.SelectedOptionId.Value && o.IsCorrect);

                if (isCorrect)
                {
                    correctCount++;
                }

                attempt.Answers.Add(new QuizAnswer
                {
                    AttemptId = attempt.Id,
                    QuestionId = answerInput.QuestionId,
                    SelectedOptionId = answerInput.SelectedOptionId,
                });
            }

            var grading = QuizGradingCalculator.Calculate(attempt.Quiz.Questions.Count, correctCount, attempt.Quiz.PassScore);
            attempt.Score = grading.Score;
            attempt.Passed = grading.Passed;
            attempt.SubmittedAt = DateTime.UtcNow;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assessment.attempt_submitted", nameof(QuizAttempt), attempt.Id,
                new { quizId = attempt.QuizId, score = grading.Score }));

            await _context.SaveChangesAsync(cancellationToken);

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
