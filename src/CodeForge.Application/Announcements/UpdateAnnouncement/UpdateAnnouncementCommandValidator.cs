using FluentValidation;

namespace CodeForge.Application.Announcements.UpdateAnnouncement
{
    public class UpdateAnnouncementCommandValidator : AbstractValidator<UpdateAnnouncementCommand>
    {
        public UpdateAnnouncementCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
        }
    }
}
