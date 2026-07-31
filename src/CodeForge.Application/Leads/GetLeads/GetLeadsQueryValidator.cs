using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Leads.GetLeads
{
    public class GetLeadsQueryValidator : AbstractValidator<GetLeadsQuery>
    {
        public GetLeadsQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        }
    }
}
