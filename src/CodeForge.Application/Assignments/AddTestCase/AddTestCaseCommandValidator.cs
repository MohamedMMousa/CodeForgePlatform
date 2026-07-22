using FluentValidation;

namespace CodeForge.Application.Assignments.AddTestCase
{
    public class AddTestCaseCommandValidator : AbstractValidator<AddTestCaseCommand>
    {
        public AddTestCaseCommandValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty();
            RuleFor(x => x.ExpectedOutput).NotEmpty().MaximumLength(10000);
            RuleFor(x => x.Input).MaximumLength(10000);
            RuleFor(x => x.Points).GreaterThan(0);
        }
    }
}
