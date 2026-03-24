using System.ComponentModel.DataAnnotations;
using Qalam.MessagingApi.Models.Enums;

namespace Qalam.MessagingApi.Models.Requests;

public class SendPushNotificationRequest
{
    [Required]
    public string DeviceToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Body { get; set; } = string.Empty;

    public Dictionary<string, object>? Data { get; set; }

    public string? Icon { get; set; }

    public string? Sound { get; set; }

    public SendingStrategy Strategy { get; set; } = SendingStrategy.Fallback;
}
