using FluentValidation;

namespace CodeForge.Application.MyCourses.GetMyCourseGrades
{
    public class GetMyCourseGradesQueryValidator : AbstractValidator<GetMyCourseGradesQuery>
    {
        public GetMyCourseGradesQueryValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
