using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Common.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeForge.Infrastructure.Notifications
{
    /// <summary>
    /// WhatsApp Business Cloud API integration point (SRS.md §10's primary target
    /// channel). Real delivery requires a Meta-verified business, a dedicated number, and
    /// pre-approved message templates — none of which exist in this environment (same
    /// class of blocker as Piston in Phase 3). While WhatsAppSettings:Enabled is false
    /// (the default), this channel no-ops with a single log line per event rather than
    /// failing — email remains the fully-working channel for every event in the catalog.
    /// Swap in real Cloud API calls here once credentials exist.
    /// </summary>
    public class WhatsAppNotificationChannel : INotificationChannel
    {
        private readonly WhatsAppSettings _settings;
        private readonly ILogger<WhatsAppNotificationChannel> _logger;

        public WhatsAppNotificationChannel(IOptions<WhatsAppSettings> options, ILogger<WhatsAppNotificationChannel> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public string ChannelName => "whatsapp";

        public Task SendAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled || string.IsNullOrWhiteSpace(notificationEvent.RecipientPhone))
            {
                _logger.LogInformation(
                    "[WhatsApp not configured] Skipped {EventType} for {RecipientName}.",
                    notificationEvent.EventType,
                    notificationEvent.RecipientName);
                return Task.CompletedTask;
            }

            // TODO(Phase 5+): call the WhatsApp Business Cloud API once
            // WhatsAppSettings:Enabled is true and PhoneNumberId/AccessToken are set.
            throw new NotImplementedException(
                "WhatsAppSettings:Enabled is true but no Cloud API integration is implemented yet.");
        }
    }
}
