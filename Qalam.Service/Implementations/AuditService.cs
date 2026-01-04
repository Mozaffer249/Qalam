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
    public class AuditService : IAuditService
    {
        private readonly IGenericRepositoryAsync<AuditLog> _auditLogRepository;
        private readonly IGenericRepositoryAsync<SecurityEvent> _securityEventRepository;

        public AuditService(
            IGenericRepositoryAsync<AuditLog> auditLogRepository,
            IGenericRepositoryAsync<SecurityEvent> securityEventRepository)
        {
            _auditLogRepository = auditLogRepository;
            _securityEventRepository = securityEventRepository;
        }

        public async Task LogAsync(string action, int? userId, string ipAddress, bool success, string? userAgent = null, string? details = null, string? failureReason = null)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = details,
                Success = success,
                FailureReason = failureReason,
                Timestamp = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(auditLog);
        }

        public async Task<List<AuditLog>> GetUserAuditLogsAsync(int userId, int pageNumber, int pageSize)
        {
            return await _auditLogRepository.GetTableNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task LogSecurityEventAsync(int userId, SecurityEventType eventType, string ipAddress, string details)
        {
            var securityEvent = new SecurityEvent
            {
                UserId = userId,
                EventType = eventType,
                IpAddress = ipAddress,
                Details = details,
                OccurredAt = DateTime.UtcNow,
                WasNotified = false
            };

            await _securityEventRepository.AddAsync(securityEvent);
        }

        public async Task<List<SecurityEvent>> GetUserSecurityEventsAsync(int userId, int pageNumber, int pageSize)
        {
            return await _securityEventRepository.GetTableNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.OccurredAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
