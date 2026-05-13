using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Service.BackgroundServices;

public class EnrollmentExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnrollmentExpirationService> _logger;
    private readonly EnrollmentSettings _settings;

    public EnrollmentExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<EnrollmentExpirationService> logger,
        IOptions<EnrollmentSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EnrollmentExpirationService started. Check interval: {Minutes} minutes.",
            _settings.ExpirationCheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExpireEnrollments(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during enrollment expiration check.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.ExpirationCheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckAndExpireEnrollments(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var enrollmentRepo = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();

        var now = DateTime.UtcNow;

        var expired = await enrollmentRepo.GetExpiredPendingPaymentAsync(now, ct);
        foreach (var enrollment in expired)
        {
            enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;

            // Mark still-pending participants as Cancelled. Already-Succeeded participants
            // stay Succeeded; refund handling is out of scope here.
            foreach (var participant in enrollment.Participants)
            {
                if (participant.PaymentStatus == PaymentStatus.Pending)
                    participant.PaymentStatus = PaymentStatus.Cancelled;
            }

            await enrollmentRepo.UpdateAsync(enrollment);
            _logger.LogInformation("Cancelled expired enrollment {Id} (CourseId: {CourseId}, Kind: {Kind}).",
                enrollment.Id, enrollment.CourseId, enrollment.Kind);
        }

        if (expired.Count > 0)
            await enrollmentRepo.SaveChangesAsync();
    }
}
