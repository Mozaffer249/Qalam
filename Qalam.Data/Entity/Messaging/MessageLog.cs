using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Messaging
{
    public class MessageLog
    {
        public Guid Id { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public MessageType Type { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MessageStatus Status { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public string? Metadata { get; set; }
    }
}
