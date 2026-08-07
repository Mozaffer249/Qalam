namespace Qalam.Service.Abstracts;

/// <summary>
/// Queues a chat-message email to a recipient, subject to a per-conversation cooldown.
/// </summary>
public interface IChatEmailNotifier
{
    /// <summary>
    /// Looks up the recipient email and queues a notification unless the cooldown
    /// for <paramref name="conversationId"/> + <paramref name="recipientUserId"/> is active.
    /// Failures are logged and never thrown to the caller.
    /// </summary>
    Task TryNotifyAsync(
        int conversationId,
        int recipientUserId,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
