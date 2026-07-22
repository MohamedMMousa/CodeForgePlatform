using FluentValidation;

namespace CodeForge.Application.Cohorts.CompleteCohort
{
    public class CompleteCohortCommandValidator : AbstractValidator<CompleteCohortCommand>
    {
        public CompleteCohortCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
