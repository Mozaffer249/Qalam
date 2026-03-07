namespace Qalam.Data.Entity.Messaging
{
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
    }
}
