using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.UpdateQuestion
{
    public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, QuestionResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateQuestionCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<QuestionResponseDto> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var question = await _context.QuizQuestions
                .Include(q => q.Quiz).ThenInclude(quiz => quiz.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

            if (question is null)
            {
                throw new KeyNotFoundException("Question was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, question.Quiz.Module.Course, currentUserId);

            var hasAnswers = await _context.QuizAnswers.AnyAsync(a => a.QuestionId == question.Id, cancellationToken);
            if (hasAnswers)
            {
                throw new InvalidOperationException("Cannot modify a question that already has recorded student answers.");
            }

            question.QuestionText = request.QuestionText.Trim();

            foreach (var existingOption in question.Options.ToList())
            {
                _context.QuizOptions.Remove(existingOption);
            }

            var newOptions = new List<QuizOption>();
            for (var i = 0; i < request.Options.Count; i++)
            {
                newOptions.Add(new QuizOption
                {
                    QuestionId = question.Id,
                    OptionText = request.Options[i].OptionText.Trim(),
                    IsCorrect = request.Options[i].IsCorrect,
                    OrderIndex = i,
                });
            }

            _context.QuizOptions.AddRange(newOptions);
            foreach (var option in newOptions)
            {
                question.Options.Add(option);
            }

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "question.updated", nameof(QuizQuestion), question.Id, new { quizId = question.QuizId }));

            await _context.SaveChangesAsync(cancellationToken);

            return new QuestionResponseDto(question.Id, "Question updated.");
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
