using Qalam.MessagingApi.Models.Enums;

namespace Qalam.MessagingApi.Configuration;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public SendingStrategy DefaultStrategy { get; set; } = SendingStrategy.Queued;
}
