using CodeForge.Application.Coupons.Common;
using MediatR;

namespace CodeForge.Application.Coupons.UpdateCoupon
{
    // Code is immutable after creation to avoid confusion with already-shared codes.
    public record UpdateCouponCommand(
        Guid Id,
        string Type,
        decimal Value,
        bool IsActive,
        DateTime? ValidFrom,
        DateTime? ValidUntil,
        int? UsageLimit) : IRequest<CouponDto>;
}
