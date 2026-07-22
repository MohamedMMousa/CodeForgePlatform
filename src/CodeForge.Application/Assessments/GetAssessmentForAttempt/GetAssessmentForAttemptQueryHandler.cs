using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.GetAssessmentForAttempt
{
    public class GetAssessmentForAttemptQueryHandler : IRequestHandler<GetAssessmentForAttemptQuery, AttemptAssessmentDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAssessmentForAttemptQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AttemptAssessmentDto> Handle(GetAssessmentForAttemptQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .Include(q => q.Questions).ThenInclude(qq => qq.Options)
                .FirstOrDefaultAsync(q => q.Id == request.AssessmentId, cancellationToken);

            if (quiz is null)
            {
                throw new KeyNotFoundException("Assessment was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, quiz.Module.Course, currentUserId);

            var attemptsUsed = await _context.QuizAttempts
                .CountAsync(a => a.QuizId == quiz.Id && a.StudentId == currentUserId, cancellationToken);

            if (quiz.MaxAttempts.HasValue && attemptsUsed >= quiz.MaxAttempts.Value)
            {
                throw new InvalidOperationException("Maximum attempts reached for this assessment.");
            }

            IEnumerable<Domain.Entities.QuizQuestion> questions = quiz.Questions.OrderBy(q => q.OrderIndex);
            if (quiz.RandomizeQuestions)
            {
                questions = questions.OrderBy(_ => Guid.NewGuid());
            }

            var questionDtos = questions.Select(q =>
            {
                IEnumerable<Domain.Entities.QuizOption> options = q.Options;
                if (quiz.RandomizeQuestions)
                {
                    options = options.OrderBy(_ => Guid.NewGuid());
                }

                return new AttemptQuestionDto(
                    q.Id,
                    q.QuestionText,
                    options.Select(o => new AttemptOptionDto(o.Id, o.OptionText)).ToList());
            }).ToList();

            return new AttemptAssessmentDto(
                quiz.Id, quiz.Type, quiz.Title, quiz.TimeLimitMinutes, quiz.MaxAttempts,
                attemptsUsed, quiz.DisableCopyPaste, questionDtos);
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
