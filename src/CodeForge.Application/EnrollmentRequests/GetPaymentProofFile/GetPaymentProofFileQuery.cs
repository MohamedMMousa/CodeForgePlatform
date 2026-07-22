using MediatR;

namespace CodeForge.Application.EnrollmentRequests.GetPaymentProofFile
{
    // Admin only (enforced at the controller).
    public record GetPaymentProofFileQuery(Guid EnrollmentRequestId) : IRequest<PaymentProofFileResult>;

    public record PaymentProofFileResult(Stream Stream, string ContentType, string FileName);
}
