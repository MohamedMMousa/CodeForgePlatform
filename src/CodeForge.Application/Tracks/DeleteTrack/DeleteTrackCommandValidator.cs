using FluentValidation;

namespace CodeForge.Application.Tracks.DeleteTrack
{
    public class DeleteTrackCommandValidator : AbstractValidator<DeleteTrackCommand>
    {
        public DeleteTrackCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
