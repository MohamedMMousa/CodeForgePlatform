using FluentValidation;

namespace CodeForge.Application.Assessments.SubmitAttempt
{
    public class SubmitAttemptCommandValidator : AbstractValidator<SubmitAttemptCommand>
    {
        public SubmitAttemptCommandValidator()
        {
            RuleFor(x => x.AttemptId).NotEmpty();

            RuleForEach(x => x.Answers).ChildRules(answer =>
            {
                answer.RuleFor(a => a.QuestionId).NotEmpty();
            });
        }
    }
}
