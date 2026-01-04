// Placeholder implementation
using Qalam.Data.Entity.Identity;
using System.Threading.Tasks;

namespace Qalam.Service.Implementations
{
    public class SecurityNotificationService : ISecurityNotificationService
    {
        public Task NotifyPasswordChangedAsync(User user, string ipAddress) => throw new System.NotImplementedException();
        public Task NotifyEmailChangedAsync(string oldEmail, string newEmail, string userName, string ipAddress) => throw new System.NotImplementedException();
        public Task NotifyNewDeviceLoginAsync(User user, string deviceInfo, string ipAddress) => throw new System.NotImplementedException();
        public Task NotifyTwoFactorEnabledAsync(User user) => throw new System.NotImplementedException();
        public Task NotifyTwoFactorDisabledAsync(User user) => throw new System.NotImplementedException();
        public Task NotifySessionTerminatedAsync(User user, string deviceInfo) => throw new System.NotImplementedException();
        public Task NotifySuspiciousActivityAsync(User user, string activity, string ipAddress) => throw new System.NotImplementedException();
        public Task NotifyAccountDeletedAsync(User user) => throw new System.NotImplementedException();
    }
}

