namespace CodeForge.Application.Coupons.Common
{
    public record CouponValidationResultDto(
        bool Valid,
        string Code,
        string? Type,
        decimal? Value,
        decimal OriginalPrice,
        decimal DiscountAmount,
        decimal FinalPrice,
        string? Message);
}
