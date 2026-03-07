using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts
{
    public interface IPushNotificationService
    {
        Task SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, object>? data = null);
        Task SendPushNotificationAsync(string deviceToken, string title, string body,
            SendingStrategy strategy, Dictionary<string, object>? data = null);
    }
}
