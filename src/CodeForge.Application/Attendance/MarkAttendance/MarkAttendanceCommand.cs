using CodeForge.Application.Attendance.Common;
using MediatR;

namespace CodeForge.Application.Attendance.MarkAttendance
{
    public record AttendanceEntryDto(Guid StudentId, string Status, string? Notes);

    public record MarkAttendanceCommand(Guid SessionId, List<AttendanceEntryDto> Entries) : IRequest<AttendanceResponseDto>;
}
