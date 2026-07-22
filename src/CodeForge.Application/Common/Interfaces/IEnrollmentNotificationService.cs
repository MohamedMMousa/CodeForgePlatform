namespace CodeForge.Application.Common.Interfaces
{
    public interface IEnrollmentNotificationService
    {
        Task NotifyEnrollmentApprovedAsync(
            string email,
            string fullName,
            string courseTitle,
            string? temporaryPassword,
            DateTime? accessExpiresAt,
            CancellationToken cancellationToken = default);

        Task NotifyEnrollmentRejectedAsync(
            string email,
            string fullName,
            string courseTitle,
            string rejectionReason,
            CancellationToken cancellationToken = default);
    }
}
