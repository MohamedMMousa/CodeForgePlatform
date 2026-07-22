using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.CreateQuestion
{
    public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, QuestionResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateQuestionCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<QuestionResponseDto> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
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

            var maxOrder = quiz.Questions.Count == 0 ? 0 : quiz.Questions.Max(q => q.OrderIndex);

            var question = new QuizQuestion
            {
                QuizId = quiz.Id,
                QuestionText = request.QuestionText.Trim(),
                OrderIndex = maxOrder + 1,
            };

            for (var i = 0; i < request.Options.Count; i++)
            {
                question.Options.Add(new QuizOption
                {
                    OptionText = request.Options[i].OptionText.Trim(),
                    IsCorrect = request.Options[i].IsCorrect,
                    OrderIndex = i,
                });
            }

            _context.QuizQuestions.Add(question);
            _context.QuizOptions.AddRange(question.Options);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "question.created", nameof(QuizQuestion), question.Id, new { quizId = quiz.Id }));

            await _context.SaveChangesAsync(cancellationToken);

            return new QuestionResponseDto(question.Id, "Question created.");
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
