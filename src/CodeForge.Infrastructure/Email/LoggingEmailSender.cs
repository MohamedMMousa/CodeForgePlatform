using CodeForge.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeForge.Infrastructure.Email
{
    /// <summary>
    /// Development/fallback email sender that logs metadata instead of delivering the
    /// message. Used when SMTP is not configured — including as the Production fallback
    /// when EmailSettings is unset, see Program.cs's startup guard for that case.
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
            // Body is deliberately never logged: it carries password-reset tokens and
            // temp passwords, and this stub is also what Production falls back to.
            _logger.LogInformation(
                "[DEV EMAIL] Would send to {ToEmail} | Subject: {Subject} (body not logged)",
                toEmail,
                subject);
            return Task.CompletedTask;
        }
    }
}
