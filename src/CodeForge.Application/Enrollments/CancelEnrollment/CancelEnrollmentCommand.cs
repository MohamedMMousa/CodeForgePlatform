using CodeForge.Application.Enrollments.Common;
using MediatR;

namespace CodeForge.Application.Enrollments.CancelEnrollment
{
    public record CancelEnrollmentCommand(
        Guid Id,
        string Reason,
        bool MarkAsRefunded) : IRequest<EnrollmentDto>;
}
