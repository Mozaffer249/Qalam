using Qalam.MessagingApi.Models.Enums;

namespace Qalam.MessagingApi.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string message);
    Task SendEmailAsync(string email, string subject, string message, SendingStrategy strategy);
}
