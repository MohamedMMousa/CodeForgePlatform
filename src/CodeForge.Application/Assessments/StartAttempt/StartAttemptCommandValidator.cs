using FluentValidation;

namespace CodeForge.Application.Assessments.StartAttempt
{
    public class StartAttemptCommandValidator : AbstractValidator<StartAttemptCommand>
    {
        public StartAttemptCommandValidator()
        {
            RuleFor(x => x.AssessmentId).NotEmpty();
        }
    }
}
