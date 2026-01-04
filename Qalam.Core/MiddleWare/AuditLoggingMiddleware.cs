using Microsoft.AspNetCore.Http;
using Qalam.Service.Abstracts;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Qalam.Core.MiddleWare
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuditService auditService)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Only log authentication-related endpoints
            if (path.Contains("/authentication/") || path.Contains("/account/"))
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("uid")?.Value;
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var uid))
                {
                    userId = uid;
                }

                var ipAddress = GetClientIpAddress(context);
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                var action = GetActionFromPath(path);

                try
                {
                    await _next(context);

                    // Log successful request
                    var success = context.Response.StatusCode >= 200 && context.Response.StatusCode < 400;
                    await auditService.LogAsync(action, userId, ipAddress, success, userAgent);
                }
                catch (Exception ex)
                {
                    // Log failed request
                    await auditService.LogAsync(action, userId, ipAddress, false, userAgent, failureReason: ex.Message);
                    throw;
                }
            }
            else
            {
                await _next(context);
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private string GetActionFromPath(string path)
        {
            if (path.Contains("/login")) return "LOGIN";
            if (path.Contains("/logout")) return "LOGOUT";
            if (path.Contains("/register")) return "REGISTER";
            if (path.Contains("/changepassword")) return "CHANGE_PASSWORD";
            if (path.Contains("/resetpassword")) return "RESET_PASSWORD";
            if (path.Contains("/confirmemail")) return "CONFIRM_EMAIL";
            if (path.Contains("/enabletwofactor")) return "ENABLE_2FA";
            if (path.Contains("/disabletwofactor")) return "DISABLE_2FA";
            if (path.Contains("/refreshtoken")) return "REFRESH_TOKEN";

            return "AUTHENTICATION_ACTION";
        }
    }
}

