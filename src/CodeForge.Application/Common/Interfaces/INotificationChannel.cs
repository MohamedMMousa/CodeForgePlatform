using CodeForge.Application.Common.Notifications;

namespace CodeForge.Application.Common.Interfaces
{
    /// <summary>
    /// One delivery mechanism (email, WhatsApp, ...). Implementations decide whether an
    /// event applies to them, how to render it, and must never let a delivery failure
    /// propagate — NotificationDispatcher already isolates callers, but channels should
    /// still fail soft internally where practical (e.g. "not configured" is not an error).
    /// </summary>
    public interface INotificationChannel
    {
        string ChannelName { get; }

        Task SendAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default);
    }
}
