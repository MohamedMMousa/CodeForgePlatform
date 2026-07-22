using FluentValidation;

namespace CodeForge.Application.Assignments.DeleteAssignment
{
    public class DeleteAssignmentCommandValidator : AbstractValidator<DeleteAssignmentCommand>
    {
        public DeleteAssignmentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
