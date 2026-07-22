using CodeForge.Application.EnrollmentRequests.Common;
using MediatR;

namespace CodeForge.Application.EnrollmentRequests.RejectEnrollmentRequest
{
    public record RejectEnrollmentRequestCommand(
        Guid Id,
        string RejectionReason) : IRequest<EnrollmentRequestMessageDto>;
}
