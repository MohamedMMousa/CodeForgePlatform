using FluentValidation;

namespace CodeForge.Application.Courses.DeleteCourse
{
    public class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
    {
        public DeleteCourseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
