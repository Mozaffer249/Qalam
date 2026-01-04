using Qalam.Data.Entity.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Qalam.Service.Abstracts
{
    public interface ISessionManagementService
    {
        Task<LoginSession> CreateSessionAsync(int userId, string deviceId, string deviceName, string ipAddress, string userAgent, string accessToken, string refreshToken, string? location = null);
        Task<List<LoginSession>> GetActiveSessionsAsync(int userId);
        Task<LoginSession?> GetSessionByIdAsync(int sessionId, int userId);
        Task<bool> TerminateSessionAsync(int sessionId, int userId);
        Task<bool> TerminateAllSessionsExceptCurrentAsync(int userId, string currentAccessToken);
        Task UpdateSessionActivityAsync(string accessToken);
        Task<bool> IsDeviceTrustedAsync(int userId, string deviceId);
        Task<TrustedDevice> AddTrustedDeviceAsync(int userId, string deviceId, string deviceName, string deviceFingerprint);
        Task<List<TrustedDevice>> GetTrustedDevicesAsync(int userId);
        Task<bool> RemoveTrustedDeviceAsync(int deviceId, int userId);
    }
}

