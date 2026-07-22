namespace CodeForge.Application.Common.Notifications
{
    /// <summary>
    /// A channel-agnostic notification fact: "this happened, to this person." Handlers
    /// build one of these and hand it to INotificationDispatcher; they never know or care
    /// which channels are configured or how the message gets rendered.
    /// </summary>
    public record NotificationEvent(
        string EventType,
        string RecipientEmail,
        string RecipientName,
        string? RecipientPhone,
        IReadOnlyDictionary<string, string> Data);
}
