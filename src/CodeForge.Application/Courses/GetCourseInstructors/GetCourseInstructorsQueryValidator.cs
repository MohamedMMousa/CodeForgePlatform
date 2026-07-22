using FluentValidation;

namespace CodeForge.Application.Courses.GetCourseInstructors
{
    public class GetCourseInstructorsQueryValidator : AbstractValidator<GetCourseInstructorsQuery>
    {
        public GetCourseInstructorsQueryValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
