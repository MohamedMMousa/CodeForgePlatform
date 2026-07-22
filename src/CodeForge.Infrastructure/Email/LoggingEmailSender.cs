using CodeForge.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeForge.Infrastructure.Email
{
    /// <summary>
    /// Development/fallback email sender that logs the message instead of delivering it.
    /// Used when SMTP is not configured, so local flows (e.g. password reset) are observable
    /// without a real mail server and without leaking tokens in API responses.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[DEV EMAIL] To: {ToEmail} | Subject: {Subject}\n{Body}",
                toEmail,
                subject,
                htmlBody);
            return Task.CompletedTask;
        }
    }
}
