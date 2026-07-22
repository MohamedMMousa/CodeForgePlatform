using FluentValidation;

namespace CodeForge.Application.Courses.RemoveInstructorFromCourse
{
    public class RemoveInstructorFromCourseCommandValidator : AbstractValidator<RemoveInstructorFromCourseCommand>
    {
        public RemoveInstructorFromCourseCommandValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
            RuleFor(x => x.InstructorId).NotEmpty();
        }
    }
}
