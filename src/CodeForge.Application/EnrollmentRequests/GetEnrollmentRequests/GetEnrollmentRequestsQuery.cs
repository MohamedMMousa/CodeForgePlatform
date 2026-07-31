using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.EnrollmentRequests.Common;
using MediatR;

namespace CodeForge.Application.EnrollmentRequests.GetEnrollmentRequests
{
    public record GetEnrollmentRequestsQuery(
        string? Status,
        Guid? CourseId,
        Guid? TrackId,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<EnrollmentRequestDto>>;
}
