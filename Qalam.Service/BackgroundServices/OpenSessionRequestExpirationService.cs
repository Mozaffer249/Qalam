using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.BackgroundServices;

/// <summary>
/// Scenario 2: expires open session requests past ExpiresAt and withdraws their pending offers.
/// Reuses <see cref="OpenSessionOfferSettings.ExpirationCheckIntervalMinutes"/>.
/// </summary>
public class OpenSessionRequestExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpenSessionRequestExpirationService> _logger;
    private readonly OpenSessionOfferSettings _settings;

    public OpenSessionRequestExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<OpenSessionRequestExpirationService> logger,
        IOptions<OpenSessionOfferSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OpenSessionRequestExpirationService started. Check interval: {Minutes} minutes.",
            _settings.ExpirationCheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OpenSessionRequest expiration sweep.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(Math.Max(1, _settings.ExpirationCheckIntervalMinutes)),
                stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var requestRepo = scope.ServiceProvider.GetRequiredService<IOpenSessionRequestRepository>();
        var rabbitMq = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var expiredIds = await requestRepo.ExpireOpenRequestsAsync(DateTime.UtcNow, ct);
        if (expiredIds.Count == 0) return;

        _logger.LogInformation("Expired {Count} open session requests.", expiredIds.Count);

        foreach (var requestId in expiredIds)
        {
            try
            {
                var summary = await requestRepo.GetStatusSummaryAsync(requestId, ct);
                if (summary == null) continue;

                var user = await userManager.FindByIdAsync(summary.RequestedByUserId.ToString());
                if (user?.Email == null) continue;

                await rabbitMq.QueueEmailAsync(new EmailMessage
                {
                    To = user.Email,
                    Subject = "انتهت صلاحية طلب الجلسات",
                    Body = "انتهت صلاحية أحد طلبات الجلسات الخاصة بك. يمكنك إنشاء طلب جديد إذا رغبت.",
                    QueuedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify on expired request {RequestId}.", requestId);
            }
        }
    }
}
