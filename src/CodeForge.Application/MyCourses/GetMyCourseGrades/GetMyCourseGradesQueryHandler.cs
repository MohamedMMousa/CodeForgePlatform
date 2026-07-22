using CodeForge.Application.Attendance.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Gradebook.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.MyCourses.GetMyCourseGrades
{
    public class GetMyCourseGradesQueryHandler : IRequestHandler<GetMyCourseGradesQuery, MyCourseGradesDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyCourseGradesQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MyCourseGradesDto> Handle(GetMyCourseGradesQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Instructors)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            CourseContentAuthorization.EnsureCanView(_currentUserService, course, currentUserId);

            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.Module.CourseId == request.CourseId)
                .ToListAsync(cancellationToken);
            var quizIds = quizzes.Select(q => q.Id).ToList();
            var attempts = await _context.QuizAttempts
                .AsNoTracking()
                .Where(a => quizIds.Contains(a.QuizId) && a.StudentId == currentUserId)
                .ToListAsync(cancellationToken);

            var assignments = await _context.Assignments
                .AsNoTracking()
                .Where(a => a.Module.CourseId == request.CourseId)
                .ToListAsync(cancellationToken);
            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = await _context.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => assignmentIds.Contains(s.AssignmentId) && s.StudentId == currentUserId)
                .ToListAsync(cancellationToken);

            var assessmentGrades = GradebookCalculator.BuildAssessmentGrades(currentUserId, quizzes, attempts);
            var assignmentGrades = GradebookCalculator.BuildAssignmentGrades(currentUserId, assignments, submissions);

            var enrollment = course.Enrollments
                .Where(e => e.StudentId == currentUserId && e.Status == EnrollmentStatuses.Active)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            var attendanceRate = 0m;
            if (enrollment is not null)
            {
                var cohort = await _context.Cohorts.AsNoTracking().FirstAsync(c => c.Id == enrollment.CohortId, cancellationToken);
                var windowStart = cohort.StartDate;
                var windowEnd = cohort.EndDate.AddDays(cohort.GracePeriodDays);
                var now = DateTime.UtcNow;

                var heldSessions = await _context.Sessions
                    .AsNoTracking()
                    .Where(s => s.Module.CourseId == request.CourseId
                        && (s.Type == SessionTypes.Live || s.Type == SessionTypes.InPerson)
                        && s.ScheduledAt != null
                        && s.ScheduledAt >= windowStart && s.ScheduledAt <= windowEnd && s.ScheduledAt <= now)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

                var statuses = await _context.AttendanceRecords
                    .AsNoTracking()
                    .Where(a => a.StudentId == currentUserId && heldSessions.Contains(a.SessionId))
                    .Select(a => a.Status)
                    .ToListAsync(cancellationToken);

                attendanceRate = AttendanceRateCalculator.Calculate(heldSessions.Count, statuses).Rate;
            }

            return new MyCourseGradesDto(course.Id, course.Title, attendanceRate, assessmentGrades, assignmentGrades);
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
