using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Coupons.Common;
using MediatR;

namespace CodeForge.Application.Coupons.GetCoupons
{
    public record GetCouponsQuery(
        bool? IsActive,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<CouponDto>>;
}
