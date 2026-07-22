namespace CodeForge.Application.Common.Notifications
{
    /// <summary>
    /// The catalog of notification-worthy business events. Adding a new one requires a
    /// matching template in EmailNotificationChannel (and, once WhatsApp is enabled, in
    /// WhatsAppNotificationChannel) — see docs/ARCHITECTURE.md §1.
    /// </summary>
    public static class NotificationEventType
    {
        public const string EnrollmentApproved = "enrollment.approved";
        public const string EnrollmentRejected = "enrollment.rejected";
        public const string CertificateIssued = "certificate.issued";
        public const string AssignmentGraded = "assignment.graded";
    }
}
