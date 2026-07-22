using System.Text.Json;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.EnrollmentRequests.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.EnrollmentRequests.RejectEnrollmentRequest
{
    public class RejectEnrollmentRequestCommandHandler
        : IRequestHandler<RejectEnrollmentRequestCommand, EnrollmentRequestMessageDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEnrollmentNotificationService _notificationService;

        public RejectEnrollmentRequestCommandHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService,
            IEnrollmentNotificationService notificationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        public async Task<EnrollmentRequestMessageDto> Handle(
            RejectEnrollmentRequestCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var adminId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            var enrollmentRequest = await _context.EnrollmentRequests
                .Include(x => x.Course)
                .Include(x => x.Track)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (enrollmentRequest is null)
            {
                throw new KeyNotFoundException("Enrollment request was not found.");
            }

            if (enrollmentRequest.Status != EnrollmentRequestStatuses.Pending)
            {
                throw new InvalidOperationException("Only pending enrollment requests can be rejected.");
            }

            if (enrollmentRequest.CouponId.HasValue)
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(x => x.Id == enrollmentRequest.CouponId.Value, cancellationToken);
                if (coupon is not null)
                {
                    coupon.UsedCount = Math.Max(0, coupon.UsedCount - 1);
                }
            }

            enrollmentRequest.Status = EnrollmentRequestStatuses.Rejected;
            enrollmentRequest.RejectionReason = request.RejectionReason.Trim();
            enrollmentRequest.ReviewedById = adminId;
            enrollmentRequest.ReviewedAt = DateTime.UtcNow;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = adminId,
                Action = "enrollment_request.rejected",
                EntityType = nameof(EnrollmentRequest),
                EntityId = enrollmentRequest.Id,
                Metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    enrollmentRequestId = enrollmentRequest.Id,
                    courseId = enrollmentRequest.CourseId,
                    applicantEmail = enrollmentRequest.ApplicantEmail,
                    rejectionReason = enrollmentRequest.RejectionReason
                }))
            });

            await _context.SaveChangesAsync(cancellationToken);

            await _notificationService.NotifyEnrollmentRejectedAsync(
                enrollmentRequest.ApplicantEmail,
                enrollmentRequest.ApplicantName,
                enrollmentRequest.Course?.Title ?? enrollmentRequest.Track?.Title ?? "your enrollment",
                enrollmentRequest.RejectionReason,
                cancellationToken);

            return new EnrollmentRequestMessageDto(
                enrollmentRequest.Id,
                "Enrollment request rejected.");
        }
    }
}
