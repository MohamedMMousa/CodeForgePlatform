using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.CreateAssessment
{
    public class CreateAssessmentCommandHandler : IRequestHandler<CreateAssessmentCommand, AssessmentResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateAssessmentCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssessmentResponseDto> Handle(CreateAssessmentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var module = await _context.Modules
                .Include(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(m => m.Quizzes)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new KeyNotFoundException("Module was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, module.Course, currentUserId);

            var maxOrder = module.Quizzes.Count == 0 ? 0 : module.Quizzes.Max(q => q.OrderIndex);

            var quiz = new Quiz
            {
                ModuleId = module.Id,
                Type = request.Type,
                Title = request.Title.Trim(),
                OrderIndex = maxOrder + 1,
                TimeLimitMinutes = request.TimeLimitMinutes,
                PassScore = request.PassScore,
                IsPractice = request.IsPractice,
                MaxAttempts = request.MaxAttempts,
                RandomizeQuestions = request.RandomizeQuestions,
                DisableCopyPaste = request.DisableCopyPaste,
            };

            _context.Quizzes.Add(quiz);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assessment.created", nameof(Quiz), quiz.Id,
                new { quiz.Title, quiz.Type, moduleId = module.Id }));

            await _context.SaveChangesAsync(cancellationToken);

            return new AssessmentResponseDto(quiz.Id, "Assessment created.");
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
