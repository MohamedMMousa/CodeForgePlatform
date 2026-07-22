using System.Net;
using System.Net.Mail;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace CodeForge.Infrastructure.Email
{
    /// <summary>
    /// Sends email over SMTP using the configured <see cref="EmailSettings"/>.
    /// Selected only when EmailSettings:Enabled is true and a host is configured.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public SmtpEmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = string.IsNullOrWhiteSpace(_settings.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_settings.Username, _settings.Password)
            };

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
