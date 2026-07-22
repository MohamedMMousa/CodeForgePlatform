using FluentValidation;

namespace CodeForge.Application.Modules.DeleteModule
{
    public class DeleteModuleCommandValidator : AbstractValidator<DeleteModuleCommand>
    {
        public DeleteModuleCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
