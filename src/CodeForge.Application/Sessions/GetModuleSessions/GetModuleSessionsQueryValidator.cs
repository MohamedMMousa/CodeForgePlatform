using FluentValidation;

namespace CodeForge.Application.Sessions.GetModuleSessions
{
    public class GetModuleSessionsQueryValidator : AbstractValidator<GetModuleSessionsQuery>
    {
        public GetModuleSessionsQueryValidator()
        {
            RuleFor(x => x.ModuleId).NotEmpty();
        }
    }
}
