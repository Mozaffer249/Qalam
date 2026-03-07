namespace Qalam.Data.Helpers
{
    public class SmsSettings
    {
        public string Provider { get; set; } = "Twilio";
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
    }
}
