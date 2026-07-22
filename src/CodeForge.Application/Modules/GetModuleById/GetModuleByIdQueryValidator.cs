using FluentValidation;

namespace CodeForge.Application.Modules.GetModuleById
{
    public class GetModuleByIdQueryValidator : AbstractValidator<GetModuleByIdQuery>
    {
        public GetModuleByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
