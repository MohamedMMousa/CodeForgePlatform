using FluentValidation;

namespace CodeForge.Application.Assignments.DeleteTestCase
{
    public class DeleteTestCaseCommandValidator : AbstractValidator<DeleteTestCaseCommand>
    {
        public DeleteTestCaseCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
