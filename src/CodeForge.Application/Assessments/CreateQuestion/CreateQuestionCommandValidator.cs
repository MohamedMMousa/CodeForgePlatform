using FluentValidation;

namespace CodeForge.Application.Assessments.CreateQuestion
{
    public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
    {
        public CreateQuestionCommandValidator()
        {
            RuleFor(x => x.AssessmentId).NotEmpty();
            RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);

            RuleFor(x => x.Options)
                .Must(options => options.Count >= 2 && options.Count <= 10)
                .WithMessage("A question requires between 2 and 10 options.");

            RuleFor(x => x.Options)
                .Must(options => options.Count(o => o.IsCorrect) == 1)
                .WithMessage("Exactly one option must be marked correct.")
                .When(x => x.Options.Count >= 2);

            RuleForEach(x => x.Options).ChildRules(option =>
            {
                option.RuleFor(o => o.OptionText).NotEmpty().MaximumLength(500);
            });
        }
    }
}
