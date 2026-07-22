using CodeForge.Application.Courses.Common;
using FluentValidation;

namespace CodeForge.Application.Courses.GetPublishedCourseDetail
{
    public class GetPublishedCourseDetailQueryValidator : AbstractValidator<GetPublishedCourseDetailQuery>
    {
        public GetPublishedCourseDetailQueryValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty()
                .MaximumLength(255)
                .Must(CourseValidationRules.IsValidSlug)
                .WithMessage("Slug must contain lowercase letters, numbers, and hyphens only.");
        }
    }
}
