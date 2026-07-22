using FluentValidation;

namespace CodeForge.Application.Gradebook.GetCourseGradebook
{
    public class GetCourseGradebookQueryValidator : AbstractValidator<GetCourseGradebookQuery>
    {
        public GetCourseGradebookQueryValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
