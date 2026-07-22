using FluentValidation;

namespace CodeForge.Application.Cohorts.GetCohortById
{
    public class GetCohortByIdQueryValidator : AbstractValidator<GetCohortByIdQuery>
    {
        public GetCohortByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
