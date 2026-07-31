using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Courses.GetPublishedCourses
{
    public class GetPublishedCoursesQueryValidator : AbstractValidator<GetPublishedCoursesQuery>
    {
        public GetPublishedCoursesQueryValidator()
        {
            RuleFor(x => x.Category)
                .MaximumLength(100);

            RuleFor(x => x.Search)
                .MaximumLength(255);

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        }
    }
}
