using FluentValidation;

namespace CodeForge.Application.Attendance.GetSessionRoster
{
    public class GetSessionRosterQueryValidator : AbstractValidator<GetSessionRosterQuery>
    {
        public GetSessionRosterQueryValidator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
        }
    }
}
