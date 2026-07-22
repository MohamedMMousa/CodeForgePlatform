namespace CodeForge.Application.Coupons.Common
{
    public record CouponDto(
        Guid Id,
        string Code,
        string Type,
        decimal Value,
        bool IsActive,
        DateTime? ValidFrom,
        DateTime? ValidUntil,
        int? UsageLimit,
        int UsedCount,
        Guid CreatedById,
        string CreatedByName,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
