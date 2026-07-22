using CodeForge.Application.Coupons.Common;
using MediatR;

namespace CodeForge.Application.Coupons.GetCoupons
{
    public record GetCouponsQuery(bool? IsActive) : IRequest<IReadOnlyList<CouponDto>>;
}
