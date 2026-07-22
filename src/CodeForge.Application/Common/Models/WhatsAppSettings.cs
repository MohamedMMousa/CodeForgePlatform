namespace CodeForge.Application.Common.Models
{
    /// <summary>
    /// WhatsApp Business Cloud API is the roadmap's primary notification channel
    /// (see docs/SRS.md §10), but it requires a Meta-verified business, a dedicated
    /// number, and pre-approved message templates — none of which exist yet. Until
    /// then <see cref="Enabled"/> stays false and WhatsAppNotificationChannel no-ops.
    /// </summary>
    public class WhatsAppSettings
    {
        public const string SectionName = "WhatsAppSettings";

        public bool Enabled { get; set; } = false;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
    }
}
