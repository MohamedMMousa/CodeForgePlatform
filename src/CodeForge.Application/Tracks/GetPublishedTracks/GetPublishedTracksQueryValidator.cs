using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Tracks.GetPublishedTracks
{
    public class GetPublishedTracksQueryValidator : AbstractValidator<GetPublishedTracksQuery>
    {
        public GetPublishedTracksQueryValidator()
        {
            RuleFor(x => x.Search)
                .MaximumLength(255);

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        }
    }
}
