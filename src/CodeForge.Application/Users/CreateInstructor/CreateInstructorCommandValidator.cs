using FluentValidation;

namespace CodeForge.Application.Users.CreateInstructor
{
    public class CreateInstructorCommandValidator : AbstractValidator<CreateInstructorCommand>
    {
        public CreateInstructorCommandValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.Phone)
                .MaximumLength(30);
        }
    }
}
