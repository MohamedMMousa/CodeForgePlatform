using CodeForge.Application.EnrollmentRequests.Common;
using MediatR;

namespace CodeForge.Application.EnrollmentRequests.GetEnrollmentRequestById
{
    public record GetEnrollmentRequestByIdQuery(Guid Id) : IRequest<EnrollmentRequestDetailDto>;
}
