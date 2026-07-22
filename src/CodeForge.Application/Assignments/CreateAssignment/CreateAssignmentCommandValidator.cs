using FluentValidation;

namespace CodeForge.Application.Assignments.CreateAssignment
{
    public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
    {
        public CreateAssignmentCommandValidator()
        {
            RuleFor(x => x.ModuleId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(10000);
            RuleFor(x => x.MaxAttempts).GreaterThan(0).When(x => x.MaxAttempts.HasValue);
            RuleFor(x => x.PassScore).InclusiveBetween(0, 100).When(x => x.PassScore.HasValue);
        }
    }
}
