using FluentValidation;

namespace CodeForge.Application.Assignments.SubmitAssignment
{
    public class SubmitAssignmentCommandValidator : AbstractValidator<SubmitAssignmentCommand>
    {
        public SubmitAssignmentCommandValidator()
        {
            RuleFor(x => x.AssignmentId).NotEmpty();
            RuleFor(x => x.Code).NotEmpty().MaximumLength(50000);
        }
    }
}
