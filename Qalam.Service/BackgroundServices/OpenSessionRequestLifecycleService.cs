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
/// Single ordered sweep for Scenario 2 OSR lifecycle:
/// close past-cutoff requests → expire orphaned offers → demote empty ReceivingOffers →
/// settle abandoned PaymentPending → expiry nudges.
/// </summary>
public class OpenSessionRequestLifecycleService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpenSessionRequestLifecycleService> _logger;
    private readonly OpenSessionRequestSettings _settings;

    public OpenSessionRequestLifecycleService(
        IServiceScopeFactory scopeFactory,
        ILogger<OpenSessionRequestLifecycleService> logger,
        IOptions<OpenSessionRequestSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = Math.Max(1, _settings.SweepIntervalMinutes);
        _logger.LogInformation(
            "OpenSessionRequestLifecycleService started. Check interval: {Minutes} minutes.",
            interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OpenSessionRequest lifecycle sweep.");
            }

            await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var requestRepo = scope.ServiceProvider.GetRequiredService<IOpenSessionRequestRepository>();
        var offerRepo = scope.ServiceProvider.GetRequiredService<IOpenSessionOfferRepository>();
        var teacherRepo = scope.ServiceProvider.GetRequiredService<ITeacherRepository>();
        var rabbitMq = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var now = DateTime.UtcNow;

        // Phase 1 — close past-cutoff requests
        var expired = await requestRepo.ExpirePastCutoffRequestsAsync(now, _settings, ct);
        if (expired.Count > 0)
        {
            _logger.LogInformation("Expired {Count} open session requests (past cutoff).", expired.Count);
            foreach (var item in expired.Where(x => x.Notify))
            {
                await TryEmailUserAsync(
                    userManager, rabbitMq, item.RequestedByUserId,
                    "انتهت صلاحية طلب الجلسات",
                    "انتهت صلاحية أحد طلبات الجلسات الخاصة بك. يمكنك إنشاء طلب جديد إذا رغبت.",
                    ct);
            }
        }

        // Phase 2 — expire orphaned pending offers
        var expiredOfferIds = await offerRepo.ExpirePendingOffersAsync(now, ct);
        if (expiredOfferIds.Count > 0)
        {
            _logger.LogInformation("Expired {Count} pending offers.", expiredOfferIds.Count);
            foreach (var offerId in expiredOfferIds)
            {
                try
                {
                    var offer = await offerRepo.GetByIdAsync(offerId);
                    if (offer == null) continue;
                    var summary = await requestRepo.GetStatusSummaryAsync(offer.SessionRequestId, ct);

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

                    if (summary != null)
                    {
                        await TryEmailUserAsync(
                            userManager, rabbitMq, summary.RequestedByUserId,
                            "انتهت صلاحية عرض على طلب جلساتك",
                            "انتهت صلاحية أحد العروض على طلبك. افتح قائمة العروض لاتخاذ القرار.",
                            ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify on expired offer {OfferId}.", offerId);
                }
            }
        }

        // Phase 3 — demote empty ReceivingOffers
        var demoted = await requestRepo.DemoteReceivingOffersWithoutLiveOffersAsync(ct);
        if (demoted.Count > 0)
            _logger.LogInformation("Demoted {Count} ReceivingOffers requests back to Active.", demoted.Count);

        // Phase 4 — settle abandoned PaymentPending
        var settled = await requestRepo.SettleAbandonedPaymentPendingAsync(ct);
        if (settled.Count > 0)
        {
            _logger.LogInformation("Settled {Count} abandoned PaymentPending requests to Expired.", settled.Count);
            foreach (var item in settled)
            {
                var instant = item.EnrollmentCancelledAt ?? now;
                if (!OpenSessionRequestExpiry.IsWithinNotificationGrace(instant, now, _settings))
                    continue;

                await TryEmailUserAsync(
                    userManager, rabbitMq, item.RequestedByUserId,
                    "انتهت صلاحية طلب الجلسات لعدم الدفع",
                    "انتهت مهلة الدفع لأحد طلباتك. يمكنك إنشاء طلب جديد إذا رغبت.",
                    ct);
            }
        }

        // Phase 5 — expiry nudges (sorted descending hours: stage 0 = 24h, stage 1 = 6h)
        var nudgeHours = (_settings.ExpiryNudgeHours ?? Array.Empty<int>())
            .Where(h => h > 0)
            .OrderByDescending(h => h)
            .ToArray();

        for (var i = 0; i < nudgeHours.Length; i++)
        {
            var hours = nudgeHours[i];
            var candidates = await requestRepo.GetExpiryNudgeCandidatesAsync(now, i, hours, ct);
            foreach (var candidate in candidates)
            {
                if (candidate.CurrentStage > i) continue;

                await TryEmailUserAsync(
                    userManager, rabbitMq, candidate.RequestedByUserId,
                    "طلب الجلسات على وشك الانتهاء",
                    $"طلب الجلسات الخاص بك سينتهي خلال أقل من {hours} ساعة. راجع العروض واتخذ قرارك.",
                    ct);

                if (candidate.TargetedTeacherId is int teacherId)
                {
                    var emails = await teacherRepo.GetEmailsByTeacherIdsAsync(new[] { teacherId }, ct);
                    foreach (var (_, email) in emails)
                    {
                        await rabbitMq.QueueEmailAsync(new EmailMessage
                        {
                            To = email,
                            Subject = "طلب موجَّه على وشك الانتهاء",
                            Body = $"طلب جلسات موجَّه إليك سينتهي خلال أقل من {hours} ساعة.",
                            QueuedAt = DateTime.UtcNow
                        });
                    }
                }

                await requestRepo.MarkExpiryNudgeStageAsync(candidate.RequestId, (byte)(i + 1), ct);
            }
        }
    }

    private async Task TryEmailUserAsync(
        UserManager<User> userManager,
        IRabbitMQService rabbitMq,
        int userId,
        string subject,
        string body,
        CancellationToken ct)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user?.Email == null) return;

            await rabbitMq.QueueEmailAsync(new EmailMessage
            {
                To = user.Email,
                Subject = subject,
                Body = body,
                QueuedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to email user {UserId} for OSR lifecycle event.", userId);
        }
    }
}
