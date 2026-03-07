namespace Qalam.Data.Entity.Messaging
{
    public class SmsMessage
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
    }
}
