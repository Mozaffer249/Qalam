namespace Qalam.Data.Entity.Common.Enums
{
    public enum MessageStatus
    {
        Queued,
        Processing,
        Sent,
        Delivered,
        Failed,
        Cancelled
    }

    public enum MessageType
    {
        Email,
        SMS,
        PushNotification
    }

    public enum SendingStrategy
    {
        Direct,
        Queued,
        Fallback
    }
}
