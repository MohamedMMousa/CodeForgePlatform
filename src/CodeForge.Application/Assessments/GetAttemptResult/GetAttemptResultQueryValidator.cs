using FluentValidation;

namespace CodeForge.Application.Assessments.GetAttemptResult
{
    public class GetAttemptResultQueryValidator : AbstractValidator<GetAttemptResultQuery>
    {
        public GetAttemptResultQueryValidator()
        {
            RuleFor(x => x.AttemptId).NotEmpty();
        }
    }
}
