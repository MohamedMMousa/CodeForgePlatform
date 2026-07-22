using CodeForge.Application.Common.Constants;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Common
{
    /// <summary>
    /// Pure discount logic shared by coupon validation (preview) and enrollment
    /// submission (application) so the two paths can never disagree.
    /// </summary>
    public static class CouponCalculator
    {
        public static bool IsValid(Coupon coupon, DateTime now)
        {
            if (!coupon.IsActive)
            {
                return false;
            }

            if (coupon.ValidFrom is not null && now < coupon.ValidFrom)
            {
                return false;
            }

            if (coupon.ValidUntil is not null && now > coupon.ValidUntil)
            {
                return false;
            }

            if (coupon.UsageLimit is not null && coupon.UsedCount >= coupon.UsageLimit)
            {
                return false;
            }

            return true;
        }

        public static decimal CalculateDiscount(Coupon coupon, decimal basePrice)
        {
            var discount = coupon.Type == CouponTypes.Percent
                ? Math.Round(basePrice * coupon.Value / 100m, 2)
                : coupon.Value;

            return Math.Clamp(discount, 0m, basePrice);
        }
    }
}
