using FluentValidation;

namespace CodeForge.Application.Materials.GetModuleMaterials
{
    public class GetModuleMaterialsQueryValidator : AbstractValidator<GetModuleMaterialsQuery>
    {
        public GetModuleMaterialsQueryValidator()
        {
            RuleFor(x => x.ModuleId).NotEmpty();
        }
    }
}
