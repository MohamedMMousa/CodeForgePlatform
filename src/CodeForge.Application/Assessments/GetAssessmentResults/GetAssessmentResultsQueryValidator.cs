using FluentValidation;

namespace CodeForge.Application.Assessments.GetAssessmentResults
{
    public class GetAssessmentResultsQueryValidator : AbstractValidator<GetAssessmentResultsQuery>
    {
        public GetAssessmentResultsQueryValidator()
        {
            RuleFor(x => x.AssessmentId).NotEmpty();
        }
    }
}
