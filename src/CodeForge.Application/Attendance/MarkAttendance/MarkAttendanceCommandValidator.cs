using CodeForge.Application.Attendance.Common;
using FluentValidation;

namespace CodeForge.Application.Attendance.MarkAttendance
{
    public class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
    {
        public MarkAttendanceCommandValidator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
            RuleFor(x => x.Entries).NotEmpty();

            RuleForEach(x => x.Entries).ChildRules(entry =>
            {
                entry.RuleFor(e => e.StudentId).NotEmpty();
                entry.RuleFor(e => e.Status)
                    .NotEmpty()
                    .Must(status => AttendanceValidationRules.ValidStatuses.Contains(status))
                    .WithMessage("Status must be 'present', 'absent', 'late', or 'excused'.");
                entry.RuleFor(e => e.Notes).MaximumLength(2000);
            });
        }
    }
}
