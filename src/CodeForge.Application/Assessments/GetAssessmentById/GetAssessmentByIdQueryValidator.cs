using FluentValidation;

namespace CodeForge.Application.Assessments.GetAssessmentById
{
    public class GetAssessmentByIdQueryValidator : AbstractValidator<GetAssessmentByIdQuery>
    {
        public GetAssessmentByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
