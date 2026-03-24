using System.ComponentModel.DataAnnotations;
using Qalam.MessagingApi.Models.Enums;

namespace Qalam.MessagingApi.Models.Requests;

public class SendEmailRequest
{
    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; } = true;

    public SendingStrategy Strategy { get; set; } = SendingStrategy.Fallback;
}
