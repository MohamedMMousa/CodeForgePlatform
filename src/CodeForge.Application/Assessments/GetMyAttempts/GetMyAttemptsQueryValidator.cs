using FluentValidation;

namespace CodeForge.Application.Assessments.GetMyAttempts
{
    public class GetMyAttemptsQueryValidator : AbstractValidator<GetMyAttemptsQuery>
    {
        public GetMyAttemptsQueryValidator()
        {
            RuleFor(x => x.AssessmentId).NotEmpty();
        }
    }
}
