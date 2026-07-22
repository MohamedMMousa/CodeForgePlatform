using FluentValidation;

namespace CodeForge.Application.Tracks.RemoveCourseFromTrack
{
    public class RemoveCourseFromTrackCommandValidator : AbstractValidator<RemoveCourseFromTrackCommand>
    {
        public RemoveCourseFromTrackCommandValidator()
        {
            RuleFor(x => x.TrackId).NotEmpty();
            RuleFor(x => x.CourseId).NotEmpty();
        }
    }
}
