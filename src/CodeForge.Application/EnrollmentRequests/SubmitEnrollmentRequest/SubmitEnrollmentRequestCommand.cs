using CodeForge.Application.EnrollmentRequests.Common;
using MediatR;

namespace CodeForge.Application.EnrollmentRequests.SubmitEnrollmentRequest
{
    public record SubmitEnrollmentRequestCommand(
        string FullName,
        string Email,
        string? PhoneNumber,
        Guid? CourseId,
        Guid? TrackId,
        string PaymentMethod,
        string? CouponCode,
        Stream PaymentProofStream,
        string PaymentProofFileName,
        string PaymentProofContentType) : IRequest<EnrollmentRequestDto>;
}
