namespace CodeForge.Application.Common.Interfaces
{
    /// <summary>
    /// Channel-agnostic outbound email abstraction. The concrete implementation
    /// (SMTP, transactional provider, or a dev logger) is selected in Infrastructure DI,
    /// so Application handlers never depend on a specific email transport.
    /// </summary>
    public interface IEmailSender
    {
        Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default);
    }
}
