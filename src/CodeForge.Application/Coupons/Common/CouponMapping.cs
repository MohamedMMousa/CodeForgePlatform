using CodeForge.Domain.Entities;

namespace CodeForge.Application.Coupons.Common
{
    public static class CouponMapping
    {
        public static CouponDto ToDto(Coupon coupon)
        {
            return new CouponDto(
                coupon.Id,
                coupon.Code,
                coupon.Type,
                coupon.Value,
                coupon.IsActive,
                coupon.ValidFrom,
                coupon.ValidUntil,
                coupon.UsageLimit,
                coupon.UsedCount,
                coupon.CreatedById,
                coupon.CreatedBy.FullName,
                coupon.CreatedAt,
                coupon.UpdatedAt);
        }
    }
}
