using CodeForge.Application.Courses.Common;
using FluentValidation;

namespace CodeForge.Application.Tracks.GetPublishedTrackDetail
{
    public class GetPublishedTrackDetailQueryValidator : AbstractValidator<GetPublishedTrackDetailQuery>
    {
        public GetPublishedTrackDetailQueryValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty()
                .MaximumLength(255)
                .Must(CourseValidationRules.IsValidSlug)
                .WithMessage("Slug must contain lowercase letters, numbers, and hyphens only.");
        }
    }
}
