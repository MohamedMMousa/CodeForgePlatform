using MediatR;

namespace CodeForge.Application.Attendance.GetMyAttendance
{
    public record MyAttendanceSessionDto(Guid SessionId, string SessionTitle, DateTime ScheduledAt, string? Status);

    public record MyAttendanceDto(
        Guid CourseId,
        string CourseTitle,
        int SessionsHeld,
        int SessionsPresent,
        decimal AttendanceRate,
        List<MyAttendanceSessionDto> Sessions);

    public record GetMyAttendanceQuery(Guid CourseId) : IRequest<MyAttendanceDto>;
}
