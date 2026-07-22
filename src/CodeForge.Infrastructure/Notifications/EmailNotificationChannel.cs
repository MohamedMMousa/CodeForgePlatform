using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Notifications;

namespace CodeForge.Infrastructure.Notifications
{
    public class EmailNotificationChannel : INotificationChannel
    {
        private readonly IEmailSender _emailSender;

        public EmailNotificationChannel(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public string ChannelName => "email";

        public async Task SendAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
        {
            var (subject, htmlBody) = EmailNotificationTemplates.Render(notificationEvent);
            await _emailSender.SendAsync(notificationEvent.RecipientEmail, subject, htmlBody, cancellationToken);
        }
    }
}
