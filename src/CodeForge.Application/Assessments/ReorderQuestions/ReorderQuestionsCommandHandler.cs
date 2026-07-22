using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.ReorderQuestions
{
    public class ReorderQuestionsCommandHandler : IRequestHandler<ReorderQuestionsCommand>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReorderQuestionsCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(ReorderQuestionsCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var quiz = await _context.Quizzes
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == request.AssessmentId, cancellationToken);

            if (quiz is null)
            {
                throw new KeyNotFoundException("Assessment was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, quiz.Module.Course, currentUserId);

            var questionIds = request.QuestionOrders.Select(x => x.QuestionId).ToList();
            var invalidQuestions = questionIds.Except(quiz.Questions.Select(q => q.Id)).ToList();
            if (invalidQuestions.Count != 0)
            {
                throw new InvalidOperationException("One or more questions do not belong to the specified assessment.");
            }

            var duplicateOrders = request.QuestionOrders.GroupBy(x => x.OrderIndex).Where(g => g.Count() > 1).ToList();
            if (duplicateOrders.Count != 0)
            {
                throw new InvalidOperationException("Duplicate order indices are not allowed.");
            }

            foreach (var questionOrder in request.QuestionOrders)
            {
                var question = quiz.Questions.First(q => q.Id == questionOrder.QuestionId);
                question.OrderIndex = questionOrder.OrderIndex;
            }

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "questions.reordered", nameof(Quiz), quiz.Id, new { quizId = quiz.Id }));

            await _context.SaveChangesAsync(cancellationToken);
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
