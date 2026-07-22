using FluentValidation;

namespace CodeForge.Application.Courses.ArchiveCourse
{
    public class ArchiveCourseCommandValidator : AbstractValidator<ArchiveCourseCommand>
    {
        public ArchiveCourseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
