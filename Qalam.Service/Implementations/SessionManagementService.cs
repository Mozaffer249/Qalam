using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.InfrastructureBases;
using Qalam.Service.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qalam.Service.Implementations
{
    public class SessionManagementService : ISessionManagementService
    {
        private readonly IGenericRepositoryAsync<LoginSession> _sessionRepository;
        private readonly IGenericRepositoryAsync<TrustedDevice> _trustedDeviceRepository;

        public SessionManagementService(
            IGenericRepositoryAsync<LoginSession> sessionRepository,
            IGenericRepositoryAsync<TrustedDevice> trustedDeviceRepository)
        {
            _sessionRepository = sessionRepository;
            _trustedDeviceRepository = trustedDeviceRepository;
        }

        public async Task<LoginSession> CreateSessionAsync(int userId, string deviceId, string deviceName, string ipAddress, string userAgent, string accessToken, string refreshToken, string? location = null)
        {
            var session = new LoginSession
            {
                UserId = userId,
                DeviceId = deviceId,
                DeviceName = deviceName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                LoginTime = DateTime.UtcNow,
                LastActivityTime = DateTime.UtcNow,
                IsActive = true,
                Location = location
            };

            await _sessionRepository.AddAsync(session);
            return session;
        }

        public async Task<List<LoginSession>> GetActiveSessionsAsync(int userId)
        {
            return await _sessionRepository.GetTableNoTracking()
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderByDescending(x => x.LoginTime)
                .ToListAsync();
        }

        public async Task<LoginSession?> GetSessionByIdAsync(int sessionId, int userId)
        {
            return await _sessionRepository.GetTableNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId);
        }

        public async Task<bool> TerminateSessionAsync(int sessionId, int userId)
        {
            var session = await _sessionRepository.GetTableNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId);

            if (session == null)
                return false;

            session.IsActive = false;
            session.LogoutTime = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            return true;
        }

        public async Task<bool> TerminateAllSessionsExceptCurrentAsync(int userId, string currentAccessToken)
        {
            var sessions = await _sessionRepository.GetTableNoTracking()
                .Where(x => x.UserId == userId && x.IsActive && x.AccessToken != currentAccessToken)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LogoutTime = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session);
            }

            return true;
        }

        public async Task UpdateSessionActivityAsync(string accessToken)
        {
            var session = await _sessionRepository.GetTableNoTracking()
                .FirstOrDefaultAsync(x => x.AccessToken == accessToken && x.IsActive);

            if (session != null)
            {
                session.LastActivityTime = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session);
            }
        }

        public async Task<bool> IsDeviceTrustedAsync(int userId, string deviceId)
        {
            return await _trustedDeviceRepository.GetTableNoTracking()
                .AnyAsync(x => x.UserId == userId && x.DeviceId == deviceId && x.IsActive);
        }

        public async Task<TrustedDevice> AddTrustedDeviceAsync(int userId, string deviceId, string deviceName, string deviceFingerprint)
        {
            var device = new TrustedDevice
            {
                UserId = userId,
                DeviceId = deviceId,
                DeviceName = deviceName,
                DeviceFingerprint = deviceFingerprint,
                TrustedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _trustedDeviceRepository.AddAsync(device);
            return device;
        }

        public async Task<List<TrustedDevice>> GetTrustedDevicesAsync(int userId)
        {
            return await _trustedDeviceRepository.GetTableNoTracking()
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderByDescending(x => x.LastUsedAt)
                .ToListAsync();
        }

        public async Task<bool> RemoveTrustedDeviceAsync(int deviceId, int userId)
        {
            var device = await _trustedDeviceRepository.GetTableNoTracking()
                .FirstOrDefaultAsync(x => x.Id == deviceId && x.UserId == userId);

            if (device == null)
                return false;

            device.IsActive = false;
            await _trustedDeviceRepository.UpdateAsync(device);

            return true;
        }
    }
}
