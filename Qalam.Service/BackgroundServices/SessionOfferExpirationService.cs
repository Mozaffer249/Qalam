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
/// Scenario 2 P4: sweeps OpenSessionOffer rows that are still Pending past their ExpiresAt and
/// flips them to Expired. Mirrors EnrollmentExpirationService.cs in structure. Per-tick: one
/// repo call to expire + one notification per affected (teacher + student) pair.
/// </summary>
public class SessionOfferExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionOfferExpirationService> _logger;
    private readonly OpenSessionOfferSettings _settings;

    public SessionOfferExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionOfferExpirationService> logger,
        IOptions<OpenSessionOfferSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SessionOfferExpirationService started. Check interval: {Minutes} minutes.",
            _settings.ExpirationCheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SessionOffer expiration sweep.");
            }

            await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _settings.ExpirationCheckIntervalMinutes)), stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var offerRepo = scope.ServiceProvider.GetRequiredService<IOpenSessionOfferRepository>();
        var requestRepo = scope.ServiceProvider.GetRequiredService<IOpenSessionRequestRepository>();
        var teacherRepo = scope.ServiceProvider.GetRequiredService<ITeacherRepository>();
        var rabbitMq = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var expiredIds = await offerRepo.ExpirePendingOffersAsync(DateTime.UtcNow, ct);
        if (expiredIds.Count == 0) return;

        _logger.LogInformation("Expired {Count} pending offers.", expiredIds.Count);

        // Notifications. Single offers may belong to different teachers + requests; we look up
        // the parent request status summary lazily per offer.
        foreach (var offerId in expiredIds)
        {
            try
            {
                var offer = await offerRepo.GetByIdAsync(offerId);
                if (offer == null) continue;
                var summary = await requestRepo.GetStatusSummaryAsync(offer.SessionRequestId, ct);

                // Teacher email
                var teacherEmails = await teacherRepo.GetEmailsByTeacherIdsAsync(new[] { offer.TeacherId }, ct);
                foreach (var (_, teacherEmail) in teacherEmails)
                {
                    await rabbitMq.QueueEmailAsync(new EmailMessage
                    {
                        To = teacherEmail,
                        Subject = "انتهت صلاحية عرضك",
                        Body = "انتهت صلاحية أحد عروضك المعلقة. يمكنك مراجعة قائمة عروضك للتفاصيل.",
                        QueuedAt = DateTime.UtcNow
                    });
                }

                // Student / guardian email
                if (summary != null)
                {
                    var user = await userManager.FindByIdAsync(summary.RequestedByUserId.ToString());
                    if (user?.Email != null)
                    {
                        await rabbitMq.QueueEmailAsync(new EmailMessage
                        {
                            To = user.Email,
                            Subject = "انتهت صلاحية عرض على طلب جلساتك",
                            Body = "انتهت صلاحية أحد العروض على طلبك. افتح قائمة العروض لاتخاذ القرار.",
                            QueuedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify on expired offer {OfferId}.", offerId);
            }
        }
    }
}
