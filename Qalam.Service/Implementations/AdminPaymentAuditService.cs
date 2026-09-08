using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AdminPaymentAuditService : IAdminPaymentAuditService
{
    private readonly IPaymentTransactionEventRepository _events;
    private readonly IPaymentReconciliationRunRepository _runs;
    private readonly IPaymentReconciliationService _reconciliation;

    public AdminPaymentAuditService(
        IPaymentTransactionEventRepository events,
        IPaymentReconciliationRunRepository runs,
        IPaymentReconciliationService reconciliation)
    {
        _events = events;
        _runs = runs;
        _reconciliation = reconciliation;
    }

    public async Task<PagedResult<AdminPaymentTransactionEventDto>> ListEventsAsync(
        AdminPaymentTransactionEventFilter filter,
        CancellationToken cancellationToken = default)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

        PaymentTransactionEventSource? source = Enum.TryParse<PaymentTransactionEventSource>(filter.Source, true, out var s) ? s : null;
        PaymentTransactionEventType? eventType = Enum.TryParse<PaymentTransactionEventType>(filter.EventType, true, out var t) ? t : null;
        PaymentTransactionEventResult? result = Enum.TryParse<PaymentTransactionEventResult>(filter.Result, true, out var r) ? r : null;

        var q = _events.GetFilteredQuery(
            filter.PaymentId,
            filter.EnrollmentId,
            filter.EnrollmentRequestId,
            filter.OpenSessionRequestId,
            filter.Provider,
            filter.ProviderPaymentId,
            filter.ProviderInvoiceId,
            source,
            eventType,
            result,
            filter.FromUtc,
            filter.ToUtc);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminPaymentTransactionEventDto>
        {
            Items = items.Select(MapEvent).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = total
        };
    }

    public async Task<IReadOnlyList<AdminPaymentTransactionEventDto>> ListEventsForPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var items = await _events.ListForPaymentAsync(paymentId, cancellationToken);
        return items.Select(MapEvent).ToList();
    }

    public async Task<PagedResult<AdminPaymentReconciliationRunDto>> ListReconciliationRunsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _runs.GetQuery();
        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<AdminPaymentReconciliationRunDto>
        {
            Items = items.Select(MapRun).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AdminPaymentReconciliationRunDto?> GetReconciliationRunAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var run = await _runs.GetByIdAsync(id, cancellationToken);
        return run == null ? null : MapRun(run);
    }

    public Task<PaymentReconciliationRun> StartReconciliationAsync(
        StartPaymentReconciliationRequestDto request,
        int? triggeredByUserId,
        CancellationToken cancellationToken = default) =>
        _reconciliation.RunAsync(new PaymentReconciliationRequest
        {
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            ProviderPaymentIds = request.ProviderPaymentIds,
            IsScheduled = false,
            TriggeredByUserId = triggeredByUserId
        }, cancellationToken);

    private static AdminPaymentTransactionEventDto MapEvent(PaymentTransactionEvent e) => new()
    {
        Id = e.Id,
        PaymentId = e.PaymentId,
        EnrollmentId = e.EnrollmentId,
        EnrollmentRequestId = e.EnrollmentRequestId,
        OpenSessionRequestId = e.OpenSessionRequestId,
        PaymentProvider = e.PaymentProvider,
        Source = e.Source.ToString(),
        EventType = e.EventType.ToString(),
        Result = e.Result.ToString(),
        StatusBefore = e.StatusBefore?.ToString(),
        StatusAfter = e.StatusAfter?.ToString(),
        Amount = e.Amount,
        Currency = e.Currency,
        ProviderPaymentId = e.ProviderPaymentId,
        ProviderInvoiceId = e.ProviderInvoiceId,
        Notes = e.Notes,
        ErrorMessage = e.ErrorMessage,
        ReceivedAt = e.ReceivedAt
    };

    private static AdminPaymentReconciliationRunDto MapRun(PaymentReconciliationRun r) => new()
    {
        Id = r.Id,
        PaymentProvider = r.PaymentProvider,
        Source = r.Source.ToString(),
        Status = r.Status.ToString(),
        ScheduleKey = r.ScheduleKey,
        LookbackFromUtc = r.LookbackFromUtc,
        LookbackToUtc = r.LookbackToUtc,
        RemotePaymentsSeen = r.RemotePaymentsSeen,
        RemoteInvoicesSeen = r.RemoteInvoicesSeen,
        MatchedCount = r.MatchedCount,
        RepairedCount = r.RepairedCount,
        MismatchCount = r.MismatchCount,
        UnresolvedRemoteCount = r.UnresolvedRemoteCount,
        MissingRemoteCount = r.MissingRemoteCount,
        ErrorSummary = r.ErrorSummary,
        StartedAt = r.StartedAt,
        FinishedAt = r.FinishedAt
    };
}
