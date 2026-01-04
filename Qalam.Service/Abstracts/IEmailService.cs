using Qalam.Service.Models;
using System.Threading.Tasks;

namespace Qalam.Service.Abstracts
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendEmailAsync(string email, string subject, string message, EmailSendingStrategy strategy);
    }
}

