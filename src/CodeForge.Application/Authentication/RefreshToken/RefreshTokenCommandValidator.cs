using FluentValidation;

namespace CodeForge.Application.Authentication.RefreshToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .MaximumLength(255);
        }
    }
}
