namespace CodeForge.Application.EnrollmentRequests.Common
{
    public record EnrollmentRequestDto(
        Guid Id,
        string ApplicantName,
        string ApplicantEmail,
        string? ApplicantPhone,
        Guid? CourseId,
        string? CourseTitle,
        Guid? TrackId,
        string? TrackTitle,
        string PaymentMethod,
        string PaymentProofUrl,
        decimal OriginalPrice,
        string? CouponCode,
        decimal DiscountAmount,
        decimal FinalPrice,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
