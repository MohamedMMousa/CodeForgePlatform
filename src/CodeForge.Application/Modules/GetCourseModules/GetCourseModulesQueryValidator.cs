using FluentValidation;

namespace CodeForge.Application.Modules.GetCourseModules
{
    public class GetCourseModulesQueryValidator : AbstractValidator<GetCourseModulesQuery>
    {
        public GetCourseModulesQueryValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
