using MediatR;

namespace CodeForge.Application.Attendance.GetCourseAttendanceReport
{
    public record StudentAttendanceSummaryDto(
        Guid StudentId,
        string StudentName,
        Guid CohortId,
        string CohortName,
        int SessionsHeld,
        int SessionsPresent,
        decimal AttendanceRate);

    public record CourseAttendanceReportDto(Guid CourseId, string CourseTitle, List<StudentAttendanceSummaryDto> Students);

    public record GetCourseAttendanceReportQuery(Guid CourseId) : IRequest<CourseAttendanceReportDto>;
}
