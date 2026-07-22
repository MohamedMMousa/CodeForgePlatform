using FluentValidation;

namespace CodeForge.Application.Materials.DeleteMaterial
{
    public class DeleteMaterialCommandValidator : AbstractValidator<DeleteMaterialCommand>
    {
        public DeleteMaterialCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
