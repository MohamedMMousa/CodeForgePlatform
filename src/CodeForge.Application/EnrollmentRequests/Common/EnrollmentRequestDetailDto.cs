namespace CodeForge.Application.EnrollmentRequests.Common
{
    public record EnrollmentRequestTargetCohortDto(
        Guid CohortId,
        string CohortName,
        Guid CourseId,
        string CourseTitle);

    public record EnrollmentRequestDetailDto(
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
        string? RejectionReason,
        Guid? ReviewedById,
        string? ReviewedByName,
        DateTime? ReviewedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<EnrollmentRequestTargetCohortDto> TargetCohorts,
        IReadOnlyList<Guid> ResultingEnrollmentIds);
}
