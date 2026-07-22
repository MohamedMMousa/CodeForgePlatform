using FluentValidation;

namespace CodeForge.Application.Assessments.GetAssessmentForAttempt
{
    public class GetAssessmentForAttemptQueryValidator : AbstractValidator<GetAssessmentForAttemptQuery>
    {
        public GetAssessmentForAttemptQueryValidator()
        {
            RuleFor(x => x.AssessmentId).NotEmpty();
        }
    }
}
