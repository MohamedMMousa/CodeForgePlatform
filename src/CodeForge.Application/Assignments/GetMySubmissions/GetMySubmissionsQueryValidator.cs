using FluentValidation;

namespace CodeForge.Application.Assignments.GetMySubmissions
{
    public class GetMySubmissionsQueryValidator : AbstractValidator<GetMySubmissionsQuery>
    {
        public GetMySubmissionsQueryValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty();
        }
    }
}
