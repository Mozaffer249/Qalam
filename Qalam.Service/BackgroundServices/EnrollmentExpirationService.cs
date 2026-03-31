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
        var enrollmentRepo = scope.ServiceProvider.GetRequiredService<ICourseEnrollmentRepository>();
        var groupEnrollmentRepo = scope.ServiceProvider.GetRequiredService<ICourseGroupEnrollmentRepository>();

        var now = DateTime.UtcNow;

        // Expire individual enrollments
        var expiredIndividual = await enrollmentRepo.GetExpiredPendingPaymentAsync(now, ct);
        foreach (var enrollment in expiredIndividual)
        {
            enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;
            await enrollmentRepo.UpdateAsync(enrollment);
            _logger.LogInformation("Cancelled expired individual enrollment {Id} (CourseId: {CourseId}).",
                enrollment.Id, enrollment.CourseId);
        }

        if (expiredIndividual.Count > 0)
            await enrollmentRepo.SaveChangesAsync();

        // Expire group enrollments
        var expiredGroup = await groupEnrollmentRepo.GetExpiredPendingPaymentAsync(now, ct);
        foreach (var groupEnrollment in expiredGroup)
        {
            groupEnrollment.Status = EnrollmentStatus.Cancelled;
            await groupEnrollmentRepo.UpdateAsync(groupEnrollment);
            _logger.LogInformation("Cancelled expired group enrollment {Id} (CourseId: {CourseId}).",
                groupEnrollment.Id, groupEnrollment.CourseId);
        }

        if (expiredGroup.Count > 0)
            await groupEnrollmentRepo.SaveChangesAsync();
    }
}
