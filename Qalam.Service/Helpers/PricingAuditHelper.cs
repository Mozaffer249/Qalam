using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Helpers;

public static class PricingAuditHelper
{
    public static async Task LogSettingChangeAsync(
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        string action,
        string entityType,
        string entityId,
        object? before,
        object? after,
        bool success = true,
        string? failureReason = null)
    {
        var http = httpContextAccessor.HttpContext;
        var userIdClaim = http?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? http?.User.FindFirst("uid")?.Value;
        int? userId = int.TryParse(userIdClaim, out var parsed) ? parsed : null;

        var ip = http?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = http?.Request.Headers.UserAgent.ToString();

        var details = JsonSerializer.Serialize(new { before, after });

        await auditService.LogAsync(
            action,
            userId,
            ip,
            success,
            userAgent,
            details,
            failureReason,
            entityType,
            entityId);
    }
}
