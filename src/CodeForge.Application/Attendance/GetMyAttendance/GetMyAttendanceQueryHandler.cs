using CodeForge.Application.Attendance.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Attendance.GetMyAttendance
{
    public class GetMyAttendanceQueryHandler : IRequestHandler<GetMyAttendanceQuery, MyAttendanceDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyAttendanceQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MyAttendanceDto> Handle(GetMyAttendanceQuery request, CancellationToken cancellationToken)
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

            var enrollment = course.Enrollments
                .Where(e => e.StudentId == currentUserId && e.Status == EnrollmentStatuses.Active)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            if (enrollment is null)
            {
                return new MyAttendanceDto(course.Id, course.Title, 0, 0, 0m, new List<MyAttendanceSessionDto>());
            }

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
                .OrderBy(s => s.ScheduledAt)
                .ToListAsync(cancellationToken);

            var sessionIds = heldSessions.Select(s => s.Id).ToList();
            var records = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.StudentId == currentUserId && sessionIds.Contains(a.SessionId))
                .ToListAsync(cancellationToken);

            var sessionDtos = heldSessions.Select(s =>
            {
                var record = records.FirstOrDefault(r => r.SessionId == s.Id);
                return new MyAttendanceSessionDto(s.Id, s.Title, s.ScheduledAt!.Value, record?.Status);
            }).ToList();

            var result = AttendanceRateCalculator.Calculate(heldSessions.Count, records.Select(r => r.Status).ToList());

            return new MyAttendanceDto(course.Id, course.Title, result.EffectiveHeld, result.PresentCount, result.Rate, sessionDtos);
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
