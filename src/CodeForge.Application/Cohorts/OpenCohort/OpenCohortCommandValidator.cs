using FluentValidation;

namespace CodeForge.Application.Cohorts.OpenCohort
{
    public class OpenCohortCommandValidator : AbstractValidator<OpenCohortCommand>
    {
        public OpenCohortCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
