using FluentValidation;

namespace CodeForge.Application.Authentication.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty();

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(100)
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("New password must be different from the current password.");
        }
    }
}
