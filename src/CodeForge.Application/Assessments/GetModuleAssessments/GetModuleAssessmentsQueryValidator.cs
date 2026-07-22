using FluentValidation;

namespace CodeForge.Application.Assessments.GetModuleAssessments
{
    public class GetModuleAssessmentsQueryValidator : AbstractValidator<GetModuleAssessmentsQuery>
    {
        public GetModuleAssessmentsQueryValidator()
        {
            RuleFor(x => x.ModuleId).NotEmpty();
        }
    }
}
