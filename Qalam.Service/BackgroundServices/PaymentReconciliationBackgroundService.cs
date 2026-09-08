using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.BackgroundServices;

public class PaymentReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentReconciliationBackgroundService> _logger;
    private readonly PaymentSettings _settings;

    public PaymentReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentReconciliationBackgroundService> logger,
        IOptions<PaymentSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Reconciliation.Enabled)
        {
            _logger.LogInformation("PaymentReconciliationBackgroundService disabled via settings.");
            return;
        }

        var interval = Math.Max(5, _settings.Reconciliation.IntervalMinutes);
        _logger.LogInformation(
            "PaymentReconciliationBackgroundService started. Interval={Minutes}m Lookback={Days}d",
            interval,
            _settings.Reconciliation.LookbackDays);

        // Stagger first run slightly so API can finish bootstrapping.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled payment reconciliation failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var reconciliation = scope.ServiceProvider.GetRequiredService<IPaymentReconciliationService>();
        var now = DateTime.UtcNow;
        // Truncate to interval bucket for cross-replica dedupe.
        var bucket = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            (now.Minute / Math.Max(5, _settings.Reconciliation.IntervalMinutes))
                * Math.Max(5, _settings.Reconciliation.IntervalMinutes),
            0,
            DateTimeKind.Utc);
        var scheduleKey = $"Moyasar:{bucket:yyyyMMddTHHmm}";

        var run = await reconciliation.RunAsync(new PaymentReconciliationRequest
        {
            IsScheduled = true,
            ScheduleKey = scheduleKey
        }, cancellationToken);

        _logger.LogInformation(
            "Reconciliation {RunId} status={Status} matched={Matched} repaired={Repaired} mismatch={Mismatch}",
            run.Id,
            run.Status,
            run.MatchedCount,
            run.RepairedCount,
            run.MismatchCount);
    }
}
