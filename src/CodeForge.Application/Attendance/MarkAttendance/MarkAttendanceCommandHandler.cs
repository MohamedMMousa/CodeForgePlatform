using CodeForge.Application.Attendance.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Attendance.MarkAttendance
{
    public class MarkAttendanceCommandHandler : IRequestHandler<MarkAttendanceCommand, AttendanceResponseDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkAttendanceCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AttendanceResponseDto> Handle(MarkAttendanceCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var session = await _context.Sessions
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Instructors)
                .Include(s => s.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

            if (session is null)
            {
                throw new KeyNotFoundException("Session was not found.");
            }

            CourseContentAuthorization.EnsureCanManage(_currentUserService, session.Module.Course, currentUserId);

            var studentIds = request.Entries.Select(e => e.StudentId).ToList();
            var activeStudentIds = session.Module.Course.Enrollments
                .Where(e => e.Status == EnrollmentStatuses.Active)
                .Select(e => e.StudentId)
                .ToHashSet();

            var invalidStudents = studentIds.Where(id => !activeStudentIds.Contains(id)).ToList();
            if (invalidStudents.Count != 0)
            {
                throw new InvalidOperationException("One or more students are not actively enrolled in this course.");
            }

            var existingRecords = await _context.AttendanceRecords
                .Where(a => a.SessionId == request.SessionId && studentIds.Contains(a.StudentId))
                .ToListAsync(cancellationToken);

            foreach (var entry in request.Entries)
            {
                var record = existingRecords.FirstOrDefault(r => r.StudentId == entry.StudentId);
                if (record is null)
                {
                    record = new AttendanceRecord
                    {
                        SessionId = request.SessionId,
                        StudentId = entry.StudentId,
                    };
                    _context.AttendanceRecords.Add(record);
                }

                record.Status = entry.Status;
                record.Notes = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes.Trim();
                record.MarkedById = currentUserId;
                record.MarkedAt = DateTime.UtcNow;
            }

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                currentUserId, "attendance.marked", nameof(Session), session.Id,
                new { sessionId = session.Id, count = request.Entries.Count }));

            await _context.SaveChangesAsync(cancellationToken);

            return new AttendanceResponseDto(session.Id, "Attendance marked.");
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
