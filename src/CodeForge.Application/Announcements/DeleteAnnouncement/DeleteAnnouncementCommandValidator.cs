using FluentValidation;

namespace CodeForge.Application.Announcements.DeleteAnnouncement
{
    public class DeleteAnnouncementCommandValidator : AbstractValidator<DeleteAnnouncementCommand>
    {
        public DeleteAnnouncementCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
