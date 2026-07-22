using CodeForge.Application.Common.Constants;
using CodeForge.Application.Sessions.Common;
using FluentValidation;

namespace CodeForge.Application.Sessions.UpdateSession
{
    public class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
    {
        public UpdateSessionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Description).MaximumLength(5000);

            RuleFor(x => x.Type)
                .NotEmpty()
                .Must(type => SessionValidationRules.ValidTypes.Contains(type))
                .WithMessage("Type must be 'live', 'in_person', or 'recorded_lesson'.");

            When(x => x.Type == SessionTypes.Live, () =>
            {
                RuleFor(x => x.ScheduledAt).NotEmpty().WithMessage("A live session requires a scheduled date/time.");
                RuleFor(x => x.JoinLink).NotEmpty().MaximumLength(500)
                    .WithMessage("A live session requires a join link.");
            });

            When(x => x.Type == SessionTypes.InPerson, () =>
            {
                RuleFor(x => x.ScheduledAt).NotEmpty().WithMessage("An in-person session requires a scheduled date/time.");
                RuleFor(x => x.Location).NotEmpty().MaximumLength(255)
                    .WithMessage("An in-person session requires a location.");
            });

            When(x => x.Type == SessionTypes.RecordedLesson, () =>
            {
                RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(500)
                    .WithMessage("A pre-recorded lesson requires a video URL.");
            });

            RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
        }
    }
}
