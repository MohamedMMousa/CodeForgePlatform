using FluentValidation;

namespace CodeForge.Application.MyCourses.GetMyCourseContent
{
    public class GetMyCourseContentQueryValidator : AbstractValidator<GetMyCourseContentQuery>
    {
        public GetMyCourseContentQueryValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
