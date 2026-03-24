using Qalam.MessagingApi.Models.Enums;

namespace Qalam.MessagingApi.Services.Interfaces;

public interface ISmsService
{
    Task SendSmsAsync(string phoneNumber, string content);
    Task SendSmsAsync(string phoneNumber, string content, SendingStrategy strategy);
}
