using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Qalam.Core.MiddleWare
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitSettings _settings;

        public RateLimitingMiddleware(RequestDelegate next, IOptions<RateLimitSettings> settings)
        {
            _next = next;
            _settings = settings.Value;
        }

        public async Task InvokeAsync(HttpContext context, IRateLimitingService rateLimitingService)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var ipAddress = GetClientIpAddress(context);

            // Check rate limits for specific endpoints
            if (path.Contains("/authentication/login") && !path.Contains("loginwithtwofactor"))
            {
                var key = $"login:{ipAddress}";
                var window = TimeSpan.FromMinutes(_settings.Login.WindowMinutes);

                if (!await rateLimitingService.IsAllowedAsync(key, _settings.Login.MaxAttempts, window))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        succeeded = false,
                        message = $"Too many login attempts. Please try again in {_settings.Login.WindowMinutes} minutes."
                    }));
                    return;
                }

                await rateLimitingService.IncrementAsync(key, window);
            }
            else if (path.Contains("/authentication/register"))
            {
                var key = $"register:{ipAddress}";
                var window = TimeSpan.FromMinutes(_settings.Register.WindowMinutes);

                if (!await rateLimitingService.IsAllowedAsync(key, _settings.Register.MaxAttempts, window))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        succeeded = false,
                        message = $"Too many registration attempts. Please try again in {_settings.Register.WindowMinutes} minutes."
                    }));
                    return;
                }

                await rateLimitingService.IncrementAsync(key, window);
            }
            else if (path.Contains("/authentication/sendresetpasswordcode"))
            {
                var key = $"passwordreset:{ipAddress}";
                var window = TimeSpan.FromMinutes(_settings.PasswordReset.WindowMinutes);

                if (!await rateLimitingService.IsAllowedAsync(key, _settings.PasswordReset.MaxAttempts, window))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        succeeded = false,
                        message = $"Too many password reset attempts. Please try again in {_settings.PasswordReset.WindowMinutes} minutes."
                    }));
                    return;
                }

                await rateLimitingService.IncrementAsync(key, window);
            }
            else if (path.Contains("/authentication/refreshtoken"))
            {
                var userId = context.User.FindFirst("uid")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var key = $"refreshtoken:{userId}";
                    var window = TimeSpan.FromMinutes(_settings.RefreshToken.WindowMinutes);

                    if (!await rateLimitingService.IsAllowedAsync(key, _settings.RefreshToken.MaxAttempts, window))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            succeeded = false,
                            message = "Too many refresh token requests. Please try again later."
                        }));
                        return;
                    }

                    await rateLimitingService.IncrementAsync(key, window);
                }
            }

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Try to get real IP from headers (for reverse proxy scenarios)
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

            // Fallback to connection IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}

