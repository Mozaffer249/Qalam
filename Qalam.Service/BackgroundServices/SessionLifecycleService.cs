using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.BackgroundServices;

/// <summary>
/// Sweeps CourseSchedules: auto-starts (Scheduled → InProgress) when start time is reached,
/// then auto-completes when scheduled end is reached (no end grace).
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
            "SessionLifecycleService started. Check interval: {Minutes} minutes.",
            interval);

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

        var due = await scheduleRepo.GetDueForAutoStartAsync(now, ct);
        var started = 0;
        foreach (var schedule in due)
        {
            try
            {
                await lifecycle.MarkInProgressAsync(schedule, ct);
                started++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-start CourseSchedule {ScheduleId}.", schedule.Id);
            }
        }

        if (started > 0)
            _logger.LogInformation("Auto-started {Count} CourseSchedule(s).", started);

        var overdue = await scheduleRepo.GetOverdueForAutoCompleteAsync(now, ct);
        var completed = 0;
        foreach (var schedule in overdue)
        {
            try
            {
                await lifecycle.CompleteAsync(schedule, ct);
                completed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-complete CourseSchedule {ScheduleId}.", schedule.Id);
            }
        }

        if (completed > 0)
            _logger.LogInformation("Auto-completed {Count} overdue CourseSchedule(s).", completed);
    }
}
