using FluentValidation;

namespace CodeForge.Application.Cohorts.GetCourseCohorts
{
    public class GetCourseCohortsQueryValidator : AbstractValidator<GetCourseCohortsQuery>
    {
        public GetCourseCohortsQueryValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
