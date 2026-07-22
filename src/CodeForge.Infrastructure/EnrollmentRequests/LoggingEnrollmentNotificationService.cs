using CodeForge.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeForge.Infrastructure.EnrollmentRequests
{
    public class LoggingEnrollmentNotificationService : IEnrollmentNotificationService
    {
        private readonly ILogger<LoggingEnrollmentNotificationService> _logger;

        public LoggingEnrollmentNotificationService(ILogger<LoggingEnrollmentNotificationService> logger)
        {
            _logger = logger;
        }

        public Task NotifyEnrollmentApprovedAsync(
            string email,
            string fullName,
            string courseTitle,
            string? temporaryPassword,
            DateTime? accessExpiresAt,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Enrollment approval notification generated for {Email}. Course: {CourseTitle}. Temporary password generated: {HasTemporaryPassword}. Access expires at: {AccessExpiresAt}.",
                email,
                courseTitle,
                !string.IsNullOrWhiteSpace(temporaryPassword),
                accessExpiresAt);

            return Task.CompletedTask;
        }

        public Task NotifyEnrollmentRejectedAsync(
            string email,
            string fullName,
            string courseTitle,
            string rejectionReason,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Enrollment rejection notification generated for {Email}. Course: {CourseTitle}. Reason: {RejectionReason}.",
                email,
                courseTitle,
                rejectionReason);

            return Task.CompletedTask;
        }
    }
}
