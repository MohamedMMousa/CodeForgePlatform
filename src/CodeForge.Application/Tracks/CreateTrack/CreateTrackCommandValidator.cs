using CodeForge.Application.Courses.Common;
using FluentValidation;

namespace CodeForge.Application.Tracks.CreateTrack
{
    public class CreateTrackCommandValidator : AbstractValidator<CreateTrackCommand>
    {
        public CreateTrackCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Slug)
                .NotEmpty()
                .MaximumLength(255)
                .Must(CourseValidationRules.IsValidSlug)
                .WithMessage("Slug must contain lowercase letters, numbers, and hyphens only.");

            RuleFor(x => x.Description)
                .MaximumLength(5000);

            RuleFor(x => x.ThumbnailUrl)
                .MaximumLength(500);

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Currency)
                .NotEmpty()
                .MaximumLength(10);
        }
    }
}
