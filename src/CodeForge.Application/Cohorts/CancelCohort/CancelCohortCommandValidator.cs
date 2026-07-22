using FluentValidation;

namespace CodeForge.Application.Cohorts.CancelCohort
{
    public class CancelCohortCommandValidator : AbstractValidator<CancelCohortCommand>
    {
        public CancelCohortCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
