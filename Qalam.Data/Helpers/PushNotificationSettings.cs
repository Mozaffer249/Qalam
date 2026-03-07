namespace Qalam.Data.Helpers
{
    public class PushNotificationSettings
    {
        public string Provider { get; set; } = "Firebase";
        public string ServerKey { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string ServiceAccountKeyPath { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
    }
}
