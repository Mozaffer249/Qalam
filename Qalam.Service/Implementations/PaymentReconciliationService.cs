using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public partial class PaymentReconciliationService : IPaymentReconciliationService
{
    private readonly IPaymentReconciliationRunRepository _runs;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly IPaymentConfirmationService _confirmation;
    private readonly IPaymentTransactionEventService _events;
    private readonly ApplicationDBContext _db;
    private readonly PaymentSettings _settings;
    private readonly ILogger<PaymentReconciliationService> _logger;

    public PaymentReconciliationService(
        IPaymentReconciliationRunRepository runs,
        IPaymentRepository payments,
        IPaymentGatewayResolver gatewayResolver,
        IPaymentConfirmationService confirmation,
        IPaymentTransactionEventService events,
        ApplicationDBContext db,
        IOptions<PaymentSettings> settings,
        ILogger<PaymentReconciliationService> logger)
    {
        _runs = runs;
        _payments = payments;
        _gatewayResolver = gatewayResolver;
        _confirmation = confirmation;
        _events = events;
        _db = db;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PaymentReconciliationRun> RunAsync(
        PaymentReconciliationRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var lookbackDays = Math.Max(1, _settings.Reconciliation.LookbackDays);
        var from = request.FromUtc ?? now.AddDays(-lookbackDays);
        var to = request.ToUtc ?? now;

        if (request.IsScheduled && !string.IsNullOrWhiteSpace(request.ScheduleKey)
            && await _runs.ScheduleKeyExistsAsync(request.ScheduleKey, cancellationToken))
        {
            _logger.LogInformation("Skipping duplicate reconciliation schedule key {Key}", request.ScheduleKey);
            return new PaymentReconciliationRun
            {
                ScheduleKey = request.ScheduleKey,
                Status = PaymentReconciliationRunStatus.Succeeded,
                Source = PaymentReconciliationRunSource.Scheduled,
                PaymentProvider = MoyasarPaymentGateway.Name,
                LookbackFromUtc = from,
                LookbackToUtc = to,
                StartedAt = now,
                FinishedAt = now,
                ErrorSummary = "duplicate_schedule_key"
            };
        }

        var run = new PaymentReconciliationRun
        {
            PaymentProvider = MoyasarPaymentGateway.Name,
            Source = request.IsScheduled
                ? PaymentReconciliationRunSource.Scheduled
                : PaymentReconciliationRunSource.Manual,
            Status = PaymentReconciliationRunStatus.Running,
            ScheduleKey = request.IsScheduled ? request.ScheduleKey : null,
            LookbackFromUtc = from,
            LookbackToUtc = to,
            StartedAt = now,
            TriggeredByUserId = request.TriggeredByUserId,
            CreatedAt = now
        };

        await _runs.AddAsync(run, cancellationToken);

        try
        {
            await ExecuteReconciliationAsync(run, request, cancellationToken);
            run.Status = run.MismatchCount > 0 || run.UnresolvedRemoteCount > 0
                ? PaymentReconciliationRunStatus.PartiallySucceeded
                : PaymentReconciliationRunStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment reconciliation run {RunId} failed", run.Id);
            run.Status = PaymentReconciliationRunStatus.Failed;
            run.ErrorSummary = Truncate(ex.Message, 2000);
        }

        run.FinishedAt = DateTime.UtcNow;
        run.UpdatedAt = run.FinishedAt;
        await _runs.UpdateAsync(run, cancellationToken);
        return run;
    }

    private static string? Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
}
