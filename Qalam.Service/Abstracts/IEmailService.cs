using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendEmailAsync(string email, string subject, string message, SendingStrategy strategy);
    }
}
