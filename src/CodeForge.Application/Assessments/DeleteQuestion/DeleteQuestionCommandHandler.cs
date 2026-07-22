using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.DeleteQuestion
{
    public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand, QuestionResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteQuestionCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<QuestionResponseDto> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var question = await _context.QuizQuestions
                .Include(q => q.Quiz).ThenInclude(quiz => quiz.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

            if (question is null)
            {
                throw new KeyNotFoundException("Question was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, question.Quiz.Module.Course, currentUserId);

            var hasAnswers = await _context.QuizAnswers.AnyAsync(a => a.QuestionId == question.Id, cancellationToken);
            if (hasAnswers)
            {
                throw new InvalidOperationException("Cannot delete a question that already has recorded student answers.");
            }

            _context.QuizQuestions.Remove(question);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "question.deleted", nameof(QuizQuestion), question.Id, new { quizId = question.QuizId }));

            await _context.SaveChangesAsync(cancellationToken);

            return new QuestionResponseDto(question.Id, "Question deleted.");
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
