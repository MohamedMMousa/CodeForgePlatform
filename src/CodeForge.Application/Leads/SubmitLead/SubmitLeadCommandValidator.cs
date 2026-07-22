using FluentValidation;

namespace CodeForge.Application.Leads.SubmitLead
{
    public class SubmitLeadCommandValidator : AbstractValidator<SubmitLeadCommand>
    {
        public SubmitLeadCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.Phone)
                .MaximumLength(20);

            RuleFor(x => x.Message)
                .MaximumLength(2000);
        }
    }
}
