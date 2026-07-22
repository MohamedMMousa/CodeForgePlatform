using FluentValidation;

namespace CodeForge.Application.Assignments.GetSubmissionsForGrading
{
    public class GetSubmissionsForGradingQueryValidator : AbstractValidator<GetSubmissionsForGradingQuery>
    {
        public GetSubmissionsForGradingQueryValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty();
        }
    }
}
