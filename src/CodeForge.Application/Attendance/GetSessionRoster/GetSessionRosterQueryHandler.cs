using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Attendance.GetSessionRoster
{
    public class GetSessionRosterQueryHandler : IRequestHandler<GetSessionRosterQuery, SessionRosterDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetSessionRosterQueryHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<SessionRosterDto> Handle(GetSessionRosterQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments).ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

            if (session is null)
            {
                throw new KeyNotFoundException("Session was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, session.Module.Course, currentUserId);

            var attendanceRecords = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.SessionId == request.SessionId)
                .ToListAsync(cancellationToken);

            var students = session.Module.Course.Enrollments
                .Where(e => e.Status == EnrollmentStatuses.Active)
                .Select(e => e.Student)
                .DistinctBy(s => s.Id)
                .OrderBy(s => s.FullName)
                .Select(s =>
                {
                    var record = attendanceRecords.FirstOrDefault(a => a.StudentId == s.Id);
                    return new RosterEntryDto(s.Id, s.FullName, record?.Status, record?.Notes);
                })
                .ToList();

            return new SessionRosterDto(session.Id, session.Title, students);
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
