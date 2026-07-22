using CodeForge.Application.Attendance.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Gradebook.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Gradebook.GetCourseGradebook
{
    public class GetCourseGradebookQueryHandler : IRequestHandler<GetCourseGradebookQuery, CourseGradebookDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCourseGradebookQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseGradebookDto> Handle(GetCourseGradebookQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Instructors)
                .Include(c => c.Enrollments).ThenInclude(e => e.Student)
                .Include(c => c.Enrollments).ThenInclude(e => e.Cohort)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, course, currentUserId);

            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.Module.CourseId == request.CourseId)
                .ToListAsync(cancellationToken);
            var quizIds = quizzes.Select(q => q.Id).ToList();
            var attempts = await _context.QuizAttempts
                .AsNoTracking()
                .Where(a => quizIds.Contains(a.QuizId))
                .ToListAsync(cancellationToken);

            var assignments = await _context.Assignments
                .AsNoTracking()
                .Where(a => a.Module.CourseId == request.CourseId)
                .ToListAsync(cancellationToken);
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await _context.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => assignmentIds.Contains(s.AssignmentId))
                .ToListAsync(cancellationToken);

            var sessions = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.Module.CourseId == request.CourseId
                    && (s.Type == SessionTypes.Live || s.Type == SessionTypes.InPerson)
                    && s.ScheduledAt != null)
                .ToListAsync(cancellationToken);
            var sessionIds = sessions.Select(s => s.Id).ToList();
            var attendanceRecords = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var rows = new List<StudentGradebookRowDto>();

            foreach (var enrollment in course.Enrollments.Where(e => e.Status == EnrollmentStatuses.Active))
            {
                var windowStart = enrollment.Cohort.StartDate;
                var windowEnd = enrollment.Cohort.EndDate.AddDays(enrollment.Cohort.GracePeriodDays);

                var heldSessionIds = sessions
                    .Where(s => s.ScheduledAt!.Value >= windowStart && s.ScheduledAt.Value <= windowEnd && s.ScheduledAt.Value <= now)
                    .Select(s => s.Id)
                    .ToHashSet();

                var statuses = attendanceRecords
                    .Where(a => a.StudentId == enrollment.StudentId && heldSessionIds.Contains(a.SessionId))
                    .Select(a => a.Status)
                    .ToList();

                var attendanceRate = AttendanceRateCalculator.Calculate(heldSessionIds.Count, statuses).Rate;
                var assessmentGrades = GradebookCalculator.BuildAssessmentGrades(enrollment.StudentId, quizzes, attempts);
                var assignmentGrades = GradebookCalculator.BuildAssignmentGrades(enrollment.StudentId, assignments, submissions);

                rows.Add(new StudentGradebookRowDto(
                    enrollment.StudentId, enrollment.Student.FullName, attendanceRate, assessmentGrades, assignmentGrades));
            }

            return new CourseGradebookDto(course.Id, course.Title, rows);
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
