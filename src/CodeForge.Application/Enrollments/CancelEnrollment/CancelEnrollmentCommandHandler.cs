using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Enrollments.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Enrollments.CancelEnrollment
{
    public class CancelEnrollmentCommandHandler : IRequestHandler<CancelEnrollmentCommand, EnrollmentDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CancelEnrollmentCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<EnrollmentDto> Handle(CancelEnrollmentCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var enrollment = await _context.Enrollments
                .Include(x => x.Student)
                .Include(x => x.Course)
                .Include(x => x.Cohort)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (enrollment is null)
            {
                throw new KeyNotFoundException("Enrollment was not found.");
            }

            if (enrollment.Status != EnrollmentStatuses.Active)
            {
                throw new InvalidOperationException("Only an active enrollment can be cancelled.");
            }

            enrollment.Status = request.MarkAsRefunded ? EnrollmentStatuses.Refunded : EnrollmentStatuses.Cancelled;
            enrollment.CancelledAt = DateTime.UtcNow;
            enrollment.CancellationReason = request.Reason.Trim();
            enrollment.CancelledById = adminId;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "enrollment.cancelled", nameof(Enrollment), enrollment.Id,
                new
                {
                    enrollment.StudentId,
                    enrollment.CourseId,
                    enrollment.CohortId,
                    refunded = request.MarkAsRefunded,
                    reason = enrollment.CancellationReason
                }));

            await _context.SaveChangesAsync(cancellationToken);

            return EnrollmentMapping.ToDto(enrollment);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return userId;
        }
    }
}
