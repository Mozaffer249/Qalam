using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts
{
    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string content);
        Task SendSmsAsync(string phoneNumber, string content, SendingStrategy strategy);
    }
}
