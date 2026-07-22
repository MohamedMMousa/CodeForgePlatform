using CodeForge.Application.Courses.Common;
using FluentValidation;

namespace CodeForge.Application.Courses.GetCourses
{
    public class GetCoursesQueryValidator : AbstractValidator<GetCoursesQuery>
    {
        public GetCoursesQueryValidator()
        {
            When(x => !string.IsNullOrWhiteSpace(x.Status), () =>
            {
                RuleFor(x => x.Status!)
                    .Must(status => CourseValidationRules.ValidStatuses.Contains(status.ToLower()))
                    .WithMessage("Status must be draft, published, or archived.");
            });

            RuleFor(x => x.Category)
                .MaximumLength(100);

            RuleFor(x => x.Search)
                .MaximumLength(255);
        }
    }
}
