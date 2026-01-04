using Qalam.Data.Entity.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Qalam.Service.Abstracts
{
    public interface IAuditService
    {
        Task LogAsync(string action, int? userId, string ipAddress, bool success, string? userAgent = null, string? details = null, string? failureReason = null);
        Task<List<AuditLog>> GetUserAuditLogsAsync(int userId, int pageNumber, int pageSize);
        Task LogSecurityEventAsync(int userId, SecurityEventType eventType, string ipAddress, string details);
        Task<List<SecurityEvent>> GetUserSecurityEventsAsync(int userId, int pageNumber, int pageSize);
    }
}

