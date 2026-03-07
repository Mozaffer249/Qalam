namespace Qalam.Data.Entity.Messaging
{
    public class PushNotificationMessage
    {
        public string DeviceToken { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
        public DateTime QueuedAt { get; set; }
    }
}
