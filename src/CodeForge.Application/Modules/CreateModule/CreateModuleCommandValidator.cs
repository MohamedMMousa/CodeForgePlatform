using FluentValidation;

namespace CodeForge.Application.Modules.CreateModule
{
    public class CreateModuleCommandValidator : AbstractValidator<CreateModuleCommand>
    {
        public CreateModuleCommandValidator()
        {
            RuleFor(x => x.CourseId).NotEmpty();

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Description)
                .MaximumLength(5000);
        }
    }
}
