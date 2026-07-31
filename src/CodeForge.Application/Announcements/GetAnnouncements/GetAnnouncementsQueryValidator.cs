using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Announcements.GetAnnouncements
{
    public class GetAnnouncementsQueryValidator : AbstractValidator<GetAnnouncementsQuery>
    {
        public GetAnnouncementsQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        }
    }
}
