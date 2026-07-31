using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Courses.GetAssignedCourses
{
    public class GetAssignedCoursesQueryValidator : AbstractValidator<GetAssignedCoursesQuery>
    {
        public GetAssignedCoursesQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        }
    }
}
