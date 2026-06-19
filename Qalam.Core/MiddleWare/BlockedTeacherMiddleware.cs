using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.MiddleWare;

public class BlockedTeacherMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;

    public BlockedTeacherMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITeacherRepository teacherRepository,
        IStringLocalizer<AuthenticationResources> authLocalizer)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("uid")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        var teacher = await teacherRepository.GetByUserIdAsync(userId);
        if (teacher?.Status != TeacherStatus.Blocked)
        {
            await _next(context);
            return;
        }

        var message = authLocalizer[AuthenticationResourcesKeys.AccountBlocked].Value
            ?? "Your account has been blocked. Please contact support.";

        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.ContentType = "application/json";

        var body = new Response<object>
        {
            Succeeded = false,
            Message = message,
            StatusCode = HttpStatusCode.Forbidden,
            Errors = []
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
