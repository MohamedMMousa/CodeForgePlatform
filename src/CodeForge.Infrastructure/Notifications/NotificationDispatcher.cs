using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Notifications;
using Microsoft.Extensions.Logging;

namespace CodeForge.Infrastructure.Notifications
{
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly IEnumerable<INotificationChannel> _channels;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(IEnumerable<INotificationChannel> channels, ILogger<NotificationDispatcher> logger)
        {
            _channels = channels;
            _logger = logger;
        }

        public async Task DispatchAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
        {
            foreach (var channel in _channels)
            {
                try
                {
                    await channel.SendAsync(notificationEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    // A notification failure must never fail the business operation that
                    // triggered it (e.g. enrollment approval already succeeded and
                    // committed before this runs) — log and move on to the next channel.
                    _logger.LogError(
                        ex,
                        "Notification channel {ChannelName} failed to deliver {EventType} to {RecipientName}.",
                        channel.ChannelName,
                        notificationEvent.EventType,
                        notificationEvent.RecipientName);
                }
            }
        }
    }
}
