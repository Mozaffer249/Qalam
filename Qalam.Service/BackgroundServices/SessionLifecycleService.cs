using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.BackgroundServices;

/// <summary>
/// Sweeps CourseSchedules past (Date + EndTime + Grace) and auto-completes them with default attendance.
/// Mirrors <see cref="EnrollmentExpirationService"/>.
/// </summary>
public class SessionLifecycleService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionLifecycleService> _logger;
    private readonly SessionSettings _settings;

    public SessionLifecycleService(
        IServiceScopeFactory scopeFactory,
        ILogger<SessionLifecycleService> logger,
        IOptions<SessionSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = Math.Max(1, _settings.LifecycleCheckIntervalMinutes);
        _logger.LogInformation(
            "SessionLifecycleService started. Check interval: {Minutes} minutes, Grace: {Grace} minutes.",
            interval, _settings.GraceMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session lifecycle sweep.");
            }

            await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<ICourseScheduleRepository>();
        var lifecycle = scope.ServiceProvider.GetRequiredService<ISessionLifecycleService>();

        var now = DateTime.UtcNow;
        var overdue = await scheduleRepo.GetOverdueForAutoCompleteAsync(now, _settings.GraceMinutes, ct);
        if (overdue.Count == 0)
            return;

        foreach (var schedule in overdue)
        {
            try
            {
                await lifecycle.CompleteAsync(schedule, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-complete CourseSchedule {ScheduleId}.", schedule.Id);
            }
        }

        _logger.LogInformation("Auto-completed {Count} overdue CourseSchedule(s).", overdue.Count);
    }
}
