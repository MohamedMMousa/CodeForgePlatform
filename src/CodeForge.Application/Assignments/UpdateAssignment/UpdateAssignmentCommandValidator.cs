using CodeForge.Application.Common.Validation;
using FluentValidation;

namespace CodeForge.Application.Assignments.UpdateAssignment
{
    public class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
    {
        public UpdateAssignmentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(10000);
            RuleFor(x => x.DueAt).MustBeUtc();
            RuleFor(x => x.MaxAttempts).GreaterThan(0).When(x => x.MaxAttempts.HasValue);
            RuleFor(x => x.PassScore).InclusiveBetween(0, 100).When(x => x.PassScore.HasValue);
        }
    }
}
