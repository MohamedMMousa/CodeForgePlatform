using FluentValidation;

namespace CodeForge.Application.Sessions.ReorderSessions
{
    public class ReorderSessionsCommandValidator : AbstractValidator<ReorderSessionsCommand>
    {
        public ReorderSessionsCommandValidator()
        {
            RuleFor(x => x.ModuleId).NotEmpty();
            RuleFor(x => x.SessionOrders).NotEmpty();
        }
    }
}
