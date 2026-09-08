using Qalam.Data.Entity.Payment;

namespace Qalam.Service.Abstracts;

public interface IPaymentReconciliationService
{
    Task<PaymentReconciliationRun> RunAsync(
        PaymentReconciliationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentReconciliationRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public bool IsScheduled { get; set; }
    public string? ScheduleKey { get; set; }
    public int? TriggeredByUserId { get; set; }
    public IReadOnlyList<string>? ProviderPaymentIds { get; set; }
}
