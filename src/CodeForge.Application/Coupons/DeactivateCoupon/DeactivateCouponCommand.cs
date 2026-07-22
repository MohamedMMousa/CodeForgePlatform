using CodeForge.Application.Coupons.Common;
using MediatR;

namespace CodeForge.Application.Coupons.DeactivateCoupon
{
    public record DeactivateCouponCommand(Guid Id) : IRequest<CouponDto>;
}
