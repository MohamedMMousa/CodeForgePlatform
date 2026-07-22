using FluentValidation;

namespace CodeForge.Application.Materials.GetSessionMaterials
{
    public class GetSessionMaterialsQueryValidator : AbstractValidator<GetSessionMaterialsQuery>
    {
        public GetSessionMaterialsQueryValidator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
        }
    }
}
