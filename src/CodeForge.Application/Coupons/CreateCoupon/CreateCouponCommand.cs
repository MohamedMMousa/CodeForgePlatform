using CodeForge.Application.Coupons.Common;
using MediatR;

namespace CodeForge.Application.Coupons.CreateCoupon
{
    public record CreateCouponCommand(
        string Code,
        string Type,
        decimal Value,
        DateTime? ValidFrom,
        DateTime? ValidUntil,
        int? UsageLimit) : IRequest<CouponDto>;
}
