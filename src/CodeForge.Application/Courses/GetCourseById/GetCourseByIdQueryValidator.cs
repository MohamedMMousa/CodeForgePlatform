using FluentValidation;

namespace CodeForge.Application.Courses.GetCourseById
{
    public class GetCourseByIdQueryValidator : AbstractValidator<GetCourseByIdQuery>
    {
        public GetCourseByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
