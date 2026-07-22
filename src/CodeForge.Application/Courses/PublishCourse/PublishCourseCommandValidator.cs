using FluentValidation;

namespace CodeForge.Application.Courses.PublishCourse
{
    public class PublishCourseCommandValidator : AbstractValidator<PublishCourseCommand>
    {
        public PublishCourseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
