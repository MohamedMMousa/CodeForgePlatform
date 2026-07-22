using FluentValidation;

namespace CodeForge.Application.Leads.MarkLeadContacted
{
    public class MarkLeadContactedCommandValidator : AbstractValidator<MarkLeadContactedCommand>
    {
        public MarkLeadContactedCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
