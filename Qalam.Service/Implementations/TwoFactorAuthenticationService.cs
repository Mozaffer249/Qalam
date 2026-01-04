// Placeholder implementation
using Qalam.Service.Abstracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Qalam.Service.Implementations
{
    public class TwoFactorAuthenticationService : ITwoFactorAuthenticationService
    {
        public Task<(string QrCodeUrl, string ManualEntryKey)> EnableTwoFactorAsync(int userId) => throw new System.NotImplementedException();
        public Task<bool> VerifyAndEnableTwoFactorAsync(int userId, string code) => throw new System.NotImplementedException();
        public Task<bool> DisableTwoFactorAsync(int userId, string password) => throw new System.NotImplementedException();
        public Task<List<string>> GenerateRecoveryCodesAsync(int userId) => throw new System.NotImplementedException();
        public Task<bool> ValidateTwoFactorCodeAsync(int userId, string code) => throw new System.NotImplementedException();
        public Task<bool> UseRecoveryCodeAsync(int userId, string code) => throw new System.NotImplementedException();
    }
}

