using CodeForge.Application.Assignments.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Notifications;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Assignments.GradeSubmission
{
    public class GradeSubmissionCommandHandler : IRequestHandler<GradeSubmissionCommand, SubmissionResultDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationDispatcher _notificationDispatcher;

        public GradeSubmissionCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            INotificationDispatcher notificationDispatcher)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationDispatcher = notificationDispatcher;
        }

        public async Task<SubmissionResultDto> Handle(GradeSubmissionCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment).ThenInclude(a => a.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(s => s.Student)
                .Include(s => s.TestResults).ThenInclude(r => r.TestCase)
                .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);

            if (submission is null)
            {
                throw new KeyNotFoundException("Submission was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, submission.Assignment.Module.Course, currentUserId);

            submission.ManualScore = request.ManualScore;
            submission.ManualFeedback = string.IsNullOrWhiteSpace(request.ManualFeedback) ? null : request.ManualFeedback.Trim();
            submission.FinalScore = request.ManualScore;
            submission.GradedById = currentUserId;
            submission.GradedAt = DateTime.UtcNow;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "assignment_submission.graded", nameof(AssignmentSubmission), submission.Id,
                new { submissionId = submission.Id, manualScore = request.ManualScore }));

            await _context.SaveChangesAsync(cancellationToken);

            await _notificationDispatcher.DispatchAsync(
                new NotificationEvent(
                    NotificationEventType.AssignmentGraded,
                    submission.Student.Email,
                    submission.Student.FullName,
                    submission.Student.Phone,
                    new Dictionary<string, string>
                    {
                        ["assignmentTitle"] = submission.Assignment.Title,
                        ["courseTitle"] = submission.Assignment.Module.Course.Title,
                        ["score"] = submission.FinalScore?.ToString() ?? "",
                        ["feedback"] = submission.ManualFeedback ?? ""
                    }),
                cancellationToken);

            return SubmissionResultMapping.ToDto(submission);
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
