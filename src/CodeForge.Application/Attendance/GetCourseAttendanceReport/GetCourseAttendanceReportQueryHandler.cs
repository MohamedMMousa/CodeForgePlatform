using CodeForge.Application.Attendance.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Attendance.GetCourseAttendanceReport
{
    public class GetCourseAttendanceReportQueryHandler : IRequestHandler<GetCourseAttendanceReportQuery, CourseAttendanceReportDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetCourseAttendanceReportQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CourseAttendanceReportDto> Handle(GetCourseAttendanceReportQuery request, CancellationToken cancellationToken)
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
            var summaries = new List<StudentAttendanceSummaryDto>();

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

                var result = AttendanceRateCalculator.Calculate(heldSessionIds.Count, statuses);

                summaries.Add(new StudentAttendanceSummaryDto(
                    enrollment.StudentId,
                    enrollment.Student.FullName,
                    enrollment.CohortId,
                    enrollment.Cohort.Name,
                    result.EffectiveHeld,
                    result.PresentCount,
                    result.Rate));
            }

            return new CourseAttendanceReportDto(course.Id, course.Title, summaries);
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
