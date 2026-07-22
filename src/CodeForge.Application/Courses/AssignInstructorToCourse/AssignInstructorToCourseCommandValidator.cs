using FluentValidation;

namespace CodeForge.Application.Courses.AssignInstructorToCourse
{
    public class AssignInstructorToCourseCommandValidator : AbstractValidator<AssignInstructorToCourseCommand>
    {
        public AssignInstructorToCourseCommandValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
            RuleFor(x => x.InstructorId).NotEmpty();
        }
    }
}
