// Placeholder implementation - to be completed with actual business logic
using Qalam.Service.Models;
using System.Threading.Tasks;

namespace Qalam.Service.Implementations
{
    public class EmailService : IEmailService
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            throw new System.NotImplementedException();
        }

        public Task SendEmailAsync(string email, string subject, string message, EmailSendingStrategy strategy)
        {
            throw new System.NotImplementedException();
        }
    }
}

