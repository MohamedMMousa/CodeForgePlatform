using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Tracks.GetTracks
{
    public class GetTracksQueryValidator : AbstractValidator<GetTracksQuery>
    {
        public GetTracksQueryValidator()
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
