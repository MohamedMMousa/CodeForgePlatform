using FluentValidation;

namespace CodeForge.Application.Attendance.GetCourseAttendanceReport
{
    public class GetCourseAttendanceReportQueryValidator : AbstractValidator<GetCourseAttendanceReportQuery>
    {
        public GetCourseAttendanceReportQueryValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
