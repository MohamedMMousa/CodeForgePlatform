using FluentValidation;

namespace CodeForge.Application.Tracks.PublishTrack
{
    public class PublishTrackCommandValidator : AbstractValidator<PublishTrackCommand>
    {
        public PublishTrackCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
