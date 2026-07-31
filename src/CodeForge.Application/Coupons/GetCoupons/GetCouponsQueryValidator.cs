using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Coupons.GetCoupons
{
    public class GetCouponsQueryValidator : AbstractValidator<GetCouponsQuery>
    {
        public GetCouponsQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        }
    }
}
