using CodeForge.Application.EnrollmentRequests.Common;
using MediatR;

namespace CodeForge.Application.EnrollmentRequests.GetEnrollmentRequests
{
    public record GetEnrollmentRequestsQuery(
        string? Status,
        Guid? CourseId,
        Guid? TrackId) : IRequest<IReadOnlyList<EnrollmentRequestDto>>;
}
