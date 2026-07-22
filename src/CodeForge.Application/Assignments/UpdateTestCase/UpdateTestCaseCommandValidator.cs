using FluentValidation;

namespace CodeForge.Application.Assignments.UpdateTestCase
{
    public class UpdateTestCaseCommandValidator : AbstractValidator<UpdateTestCaseCommand>
    {
        public UpdateTestCaseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ExpectedOutput).NotEmpty().MaximumLength(10000);
            RuleFor(x => x.Input).MaximumLength(10000);
            RuleFor(x => x.Points).GreaterThan(0);
        }
    }
}
