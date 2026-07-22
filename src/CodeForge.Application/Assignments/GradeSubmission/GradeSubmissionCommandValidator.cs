using FluentValidation;

namespace CodeForge.Application.Assignments.GradeSubmission
{
    public class GradeSubmissionCommandValidator : AbstractValidator<GradeSubmissionCommand>
    {
        public GradeSubmissionCommandValidator()
        {
            RuleFor(x => x.SubmissionId).NotEmpty();
            RuleFor(x => x.ManualScore).InclusiveBetween(0, 100);
            RuleFor(x => x.ManualFeedback).MaximumLength(5000);
        }
    }
}
