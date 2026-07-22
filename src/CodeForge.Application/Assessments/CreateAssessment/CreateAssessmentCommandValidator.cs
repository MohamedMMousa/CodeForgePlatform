using CodeForge.Application.Assessments.Common;
using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Assessments.CreateAssessment
{
    public class CreateAssessmentCommandValidator : AbstractValidator<CreateAssessmentCommand>
    {
        public CreateAssessmentCommandValidator()
        {
            RuleFor(x => x.ModuleId).NotEmpty();

            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);

            RuleFor(x => x.Type)
                .NotEmpty()
                .Must(type => AssessmentValidationRules.ValidTypes.Contains(type))
                .WithMessage("Type must be 'quiz' or 'exam'.");

            RuleFor(x => x.TimeLimitMinutes).GreaterThan(0).When(x => x.TimeLimitMinutes.HasValue);
            RuleFor(x => x.PassScore).InclusiveBetween(0, 100).When(x => x.PassScore.HasValue);
            RuleFor(x => x.MaxAttempts).GreaterThan(0).When(x => x.MaxAttempts.HasValue);

            When(x => x.Type == AssessmentTypes.Exam, () =>
            {
                RuleFor(x => x.MaxAttempts)
                    .Equal(1)
                    .WithMessage("Exams allow only a single attempt.");
            });
        }
    }
}
