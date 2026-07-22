using CodeForge.Application.Common.Notifications;

namespace CodeForge.Application.Common.Interfaces
{
    /// <summary>
    /// Fans a NotificationEvent out to every registered INotificationChannel. Never
    /// throws — a notification failure must not fail the business operation that
    /// triggered it (e.g. an enrollment approval succeeds even if email delivery fails).
    /// </summary>
    public interface INotificationDispatcher
    {
        Task DispatchAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default);
    }
}
