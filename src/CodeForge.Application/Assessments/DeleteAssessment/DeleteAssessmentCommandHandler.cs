using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assessments.DeleteAssessment
{
    public class DeleteAssessmentCommandHandler : IRequestHandler<DeleteAssessmentCommand, AssessmentResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteAssessmentCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AssessmentResponseDto> Handle(DeleteAssessmentCommand request, CancellationToken cancellationToken)
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

            _context.Quizzes.Remove(quiz);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assessment.deleted", nameof(Quiz), quiz.Id, new { quiz.Title }));

            await _context.SaveChangesAsync(cancellationToken);

            return new AssessmentResponseDto(quiz.Id, "Assessment deleted.");
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
