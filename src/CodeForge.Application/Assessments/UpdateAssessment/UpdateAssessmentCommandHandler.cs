using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.UpdateAssessment
{
    public class UpdateAssessmentCommandHandler : IRequestHandler<UpdateAssessmentCommand, AssessmentResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateAssessmentCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssessmentResponseDto> Handle(UpdateAssessmentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var quiz = await _context.Quizzes
                .Include(q => q.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

            if (quiz is null)
            {
                throw new KeyNotFoundException("Assessment was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, quiz.Module.Course, currentUserId);

            quiz.Type = request.Type;
            quiz.Title = request.Title.Trim();
            quiz.TimeLimitMinutes = request.TimeLimitMinutes;
            quiz.PassScore = request.PassScore;
            quiz.IsPractice = request.IsPractice;
            quiz.MaxAttempts = request.MaxAttempts;
            quiz.RandomizeQuestions = request.RandomizeQuestions;
            quiz.DisableCopyPaste = request.DisableCopyPaste;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assessment.updated", nameof(Quiz), quiz.Id, new { quiz.Title, quiz.Type }));

            await _context.SaveChangesAsync(cancellationToken);

            return new AssessmentResponseDto(quiz.Id, "Assessment updated.");
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
