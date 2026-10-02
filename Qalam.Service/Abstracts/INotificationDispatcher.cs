namespace Qalam.Service.Abstracts;

public sealed record NotificationContent(
    string Type,
    string TitleAr,
    string TitleEn,
    string BodyAr,
    string BodyEn,
    IReadOnlyDictionary<string, object?>? Data = null);

/// <summary>
/// Writes an inbox row per recipient, then best-effort email and push. Never throws:
/// a delivery failure must not undo the business action that triggered it.
/// </summary>
public interface INotificationDispatcher
{
    Task NotifyAsync(IEnumerable<int> userIds, NotificationContent content, CancellationToken cancellationToken = default);
}
