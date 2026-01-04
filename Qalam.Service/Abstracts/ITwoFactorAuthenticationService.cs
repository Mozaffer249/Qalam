using System.Collections.Generic;
using System.Threading.Tasks;

namespace Qalam.Service.Abstracts
{
    public interface ITwoFactorAuthenticationService
    {
        Task<(string QrCodeUrl, string ManualEntryKey)> EnableTwoFactorAsync(int userId);
        Task<bool> VerifyAndEnableTwoFactorAsync(int userId, string code);
        Task<bool> DisableTwoFactorAsync(int userId, string password);
        Task<List<string>> GenerateRecoveryCodesAsync(int userId);
        Task<bool> ValidateTwoFactorCodeAsync(int userId, string code);
        Task<bool> UseRecoveryCodeAsync(int userId, string code);
    }
}

