using FluentValidation;

namespace CodeForge.Application.Tracks.ArchiveTrack
{
    public class ArchiveTrackCommandValidator : AbstractValidator<ArchiveTrackCommand>
    {
        public ArchiveTrackCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
