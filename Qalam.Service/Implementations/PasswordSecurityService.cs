// Placeholder implementation
using Qalam.Data.Helpers;
using System.Threading.Tasks;

namespace Qalam.Service.Implementations
{
    public class PasswordSecurityService : IPasswordSecurityService
    {
        public Task<bool> IsPasswordInHistoryAsync(int userId, string newPassword, int historyCount = 5) => throw new System.NotImplementedException();
        public Task AddToPasswordHistoryAsync(int userId, string passwordHash) => throw new System.NotImplementedException();
        public Task<PasswordStrength> CheckPasswordStrengthAsync(string password) => throw new System.NotImplementedException();
        public Task<bool> IsCommonPasswordAsync(string password) => throw new System.NotImplementedException();
        public Task<bool> IsPasswordExpiredAsync(int userId) => throw new System.NotImplementedException();
    }
}

