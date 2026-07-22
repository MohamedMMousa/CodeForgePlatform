using FluentValidation;

namespace CodeForge.Application.Sessions.DeleteSession
{
    public class DeleteSessionCommandValidator : AbstractValidator<DeleteSessionCommand>
    {
        public DeleteSessionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
