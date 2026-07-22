using FluentValidation;

namespace CodeForge.Application.Announcements.CreateAnnouncement
{
    public class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
    {
        public CreateAnnouncementCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
        }
    }
}
