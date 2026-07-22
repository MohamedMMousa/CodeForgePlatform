using FluentValidation;

namespace CodeForge.Application.Attendance.GetMyAttendance
{
    public class GetMyAttendanceQueryValidator : AbstractValidator<GetMyAttendanceQuery>
    {
        public GetMyAttendanceQueryValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
