using FluentValidation;

namespace CodeForge.Application.Tracks.AddCourseToTrack
{
    public class AddCourseToTrackCommandValidator : AbstractValidator<AddCourseToTrackCommand>
    {
        public AddCourseToTrackCommandValidator()
        {
            RuleFor(x => x.TrackId).NotEmpty();
            RuleFor(x => x.CourseId).NotEmpty();
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        }
    }
}
