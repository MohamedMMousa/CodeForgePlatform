using CodeForge.Application.Common.Constants;

namespace CodeForge.Application.Attendance.Common
{
    public static class AttendanceValidationRules
    {
        public static readonly string[] ValidStatuses =
        {
            AttendanceStatuses.Present,
            AttendanceStatuses.Absent,
            AttendanceStatuses.Late,
            AttendanceStatuses.Excused,
        };
    }
}
