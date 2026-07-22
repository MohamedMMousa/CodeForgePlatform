using CodeForge.Application.EnrollmentRequests.Common;
using MediatR;

namespace CodeForge.Application.EnrollmentRequests.ApproveEnrollmentRequest
{
    // Access expiry is always derived from cohort.EndDate + cohort.GracePeriodDays
    // (docs/DATABASE.md §4) — there is no manual override anymore, since a track
    // request can target multiple cohorts with different end dates.
    public record ApproveEnrollmentRequestCommand(Guid Id) : IRequest<EnrollmentApprovalResultDto>;
}
