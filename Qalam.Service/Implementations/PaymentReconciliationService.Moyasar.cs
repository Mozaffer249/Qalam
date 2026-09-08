using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Payments;

namespace Qalam.Service.Implementations;

public partial class PaymentReconciliationService
{
    private async Task ExecuteReconciliationAsync(
        PaymentReconciliationRun run,
        PaymentReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        MoyasarPaymentGateway moyasar;
        try
        {
            moyasar = (MoyasarPaymentGateway)_gatewayResolver.Resolve(MoyasarPaymentGateway.Name);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Moyasar gateway is not configured for reconciliation.", ex);
        }

        if (!moyasar.IsConfigured)
            throw new InvalidOperationException("Moyasar gateway is not configured for reconciliation.");

        // Targeted repair for known provider ids (e.g. failed webhook subjects).
        if (request.ProviderPaymentIds is { Count: > 0 })
        {
            foreach (var id in request.ProviderPaymentIds.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var remote = await moyasar.FetchAsync(id, cancellationToken);
                if (remote == null)
                {
                    run.UnresolvedRemoteCount++;
                    await RecordReconEventAsync(
                        null,
                        PaymentTransactionEventType.ReconciliationUnresolvedRemote,
                        PaymentTransactionEventResult.NotFound,
                        id,
                        null,
                        "fetch_miss",
                        request,
                        cancellationToken);
                    continue;
                }

                await ReconcileRemoteAsync(run, remote, request, cancellationToken);
            }

            return;
        }

        var maxPages = Math.Max(1, _settings.Reconciliation.MaxPagesPerRun);
        var seenRemotePaymentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int? nextPage = 1;
        for (var i = 0; i < maxPages && nextPage.HasValue; i++)
        {
            var (items, next) = await moyasar.ListPaymentsAsync(nextPage.Value, cancellationToken);
            run.PaginationCursor = nextPage;
            foreach (var remote in items)
            {
                if (remote.CreatedAt.HasValue
                    && (remote.CreatedAt < run.LookbackFromUtc || remote.CreatedAt > run.LookbackToUtc))
                    continue;

                run.RemotePaymentsSeen++;
                if (!string.IsNullOrWhiteSpace(remote.Id))
                    seenRemotePaymentIds.Add(remote.Id);
                await ReconcileRemoteAsync(run, remote, request, cancellationToken);
            }

            nextPage = next;
        }

        nextPage = 1;
        for (var i = 0; i < maxPages && nextPage.HasValue; i++)
        {
            var (items, next) = await moyasar.ListInvoicesAsync(nextPage.Value, cancellationToken);
            foreach (var remote in items)
            {
                run.RemoteInvoicesSeen++;
                if (!string.IsNullOrWhiteSpace(remote.Id) && seenRemotePaymentIds.Contains(remote.Id))
                    continue;
                await ReconcileRemoteAsync(run, remote, request, cancellationToken);
            }

            nextPage = next;
        }

        // Local pending Moyasar payments with no matching remote in this window.
        var pendingLocals = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentProvider == MoyasarPaymentGateway.Name
                        && p.Status == PaymentStatus.Pending
                        && p.CreatedAt >= run.LookbackFromUtc
                        && p.CreatedAt <= run.LookbackToUtc)
            .Select(p => new { p.Id, p.ProviderTransactionId, p.ProviderInvoiceId })
            .ToListAsync(cancellationToken);

        foreach (var local in pendingLocals)
        {
            var refs = new[] { local.ProviderTransactionId, local.ProviderInvoiceId }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var found = false;
            foreach (var r in refs)
            {
                var remote = await moyasar.FetchAsync(r!, cancellationToken);
                if (remote != null)
                {
                    found = true;
                    await ReconcileRemoteAsync(run, remote, request, cancellationToken);
                    break;
                }
            }

            if (!found)
            {
                run.MissingRemoteCount++;
                await RecordReconEventAsync(
                    local.Id,
                    PaymentTransactionEventType.ReconciliationMissingRemote,
                    PaymentTransactionEventResult.NotFound,
                    local.ProviderTransactionId,
                    local.ProviderInvoiceId,
                    "local_pending_no_remote",
                    request,
                    cancellationToken);
            }
        }
    }

    private async Task ReconcileRemoteAsync(
        PaymentReconciliationRun run,
        GatewayPaymentDto remote,
        PaymentReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        var local = await FindLocalPaymentAsync(remote, cancellationToken);
        if (local == null)
        {
            run.UnresolvedRemoteCount++;
            await RecordReconEventAsync(
                null,
                PaymentTransactionEventType.ReconciliationUnresolvedRemote,
                PaymentTransactionEventResult.NotFound,
                remote.Id,
                remote.InvoiceId,
                remote.Status,
                request,
                cancellationToken);
            return;
        }

        var expectedHalalas = MinorUnitConverter.ToHalalas(local.TotalAmount);
        if (remote.AmountHalalas > 0
            && (remote.AmountHalalas != expectedHalalas
                || (!string.IsNullOrWhiteSpace(remote.Currency)
                    && !string.Equals(remote.Currency, local.Currency, StringComparison.OrdinalIgnoreCase))))
        {
            run.MismatchCount++;
            await RecordReconEventAsync(
                local.Id,
                PaymentTransactionEventType.ReconciliationMismatch,
                PaymentTransactionEventResult.Mismatch,
                remote.Id,
                remote.InvoiceId,
                $"amount/currency remote={remote.AmountHalalas} {remote.Currency} local={expectedHalalas} {local.Currency}",
                request,
                cancellationToken);
            return;
        }

        // Never downgrade local Succeeded from ambiguous remote.
        if (local.Status == PaymentStatus.Succeeded
            && remote.MappedStatus is PaymentStatus.Pending or PaymentStatus.Failed)
        {
            run.MismatchCount++;
            await RecordReconEventAsync(
                local.Id,
                PaymentTransactionEventType.ReconciliationMismatch,
                PaymentTransactionEventResult.Mismatch,
                remote.Id,
                remote.InvoiceId,
                $"local_succeeded_remote_{remote.Status}",
                request,
                cancellationToken);
            return;
        }

        if (local.Status == PaymentStatus.Pending
            && (remote.MappedStatus == PaymentStatus.Succeeded || MoyasarStatusMapper.IsPaid(remote.Status)))
        {
            var confirmRef = !string.IsNullOrWhiteSpace(remote.Id) ? remote.Id : remote.InvoiceId;
            var outcome = await _confirmation.ConfirmFromGatewayAsync(confirmRef!, cancellationToken);
            if (outcome.Succeeded)
            {
                run.RepairedCount++;
                await RecordReconEventAsync(
                    local.Id,
                    PaymentTransactionEventType.ReconciliationRepair,
                    PaymentTransactionEventResult.Success,
                    remote.Id,
                    remote.InvoiceId,
                    "confirm_from_remote_paid",
                    request,
                    cancellationToken);
            }
            else
            {
                run.MismatchCount++;
                await RecordReconEventAsync(
                    local.Id,
                    PaymentTransactionEventType.ReconciliationMismatch,
                    PaymentTransactionEventResult.Failed,
                    remote.Id,
                    remote.InvoiceId,
                    outcome.ErrorCode ?? "repair_failed",
                    request,
                    cancellationToken);
            }

            return;
        }

        if (remote.MappedStatus == PaymentStatus.Refunded && local.Status != PaymentStatus.Refunded)
        {
            var tracked = await _payments.GetByIdWithItemsAsync(local.Id, cancellationToken);
            if (tracked != null)
            {
                tracked.Status = PaymentStatus.Refunded;
                tracked.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(remote.InvoiceId) && string.IsNullOrWhiteSpace(tracked.ProviderInvoiceId))
                    tracked.ProviderInvoiceId = remote.InvoiceId;
                await _payments.UpdateAsync(tracked);
                run.RepairedCount++;
                await RecordReconEventAsync(
                    local.Id,
                    PaymentTransactionEventType.ReconciliationRepair,
                    PaymentTransactionEventResult.Success,
                    remote.Id,
                    remote.InvoiceId,
                    "sync_refunded",
                    request,
                    cancellationToken);
                return;
            }
        }

        run.MatchedCount++;
        await RecordReconEventAsync(
            local.Id,
            PaymentTransactionEventType.ReconciliationMatch,
            PaymentTransactionEventResult.Success,
            remote.Id,
            remote.InvoiceId,
            $"local={local.Status};remote={remote.Status}",
            request,
            cancellationToken);
    }

    private async Task<Payment?> FindLocalPaymentAsync(
        GatewayPaymentDto remote,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(remote.Id))
        {
            var byId = await _payments.GetByProviderTransactionIdAsync(remote.Id, cancellationToken);
            if (byId != null) return byId;
        }

        if (!string.IsNullOrWhiteSpace(remote.InvoiceId))
        {
            var byInvoice = await _payments.GetByProviderInvoiceIdAsync(remote.InvoiceId, cancellationToken)
                ?? await _payments.GetByProviderTransactionIdAsync(remote.InvoiceId, cancellationToken);
            if (byInvoice != null) return byInvoice;
        }

        if (remote.Metadata != null
            && remote.Metadata.TryGetValue("paymentId", out var localPaymentIdRaw)
            && int.TryParse(localPaymentIdRaw, out var localPaymentId))
        {
            return await _payments.GetByIdWithItemsAsync(localPaymentId, cancellationToken);
        }

        return null;
    }

    private async Task RecordReconEventAsync(
        int? paymentId,
        PaymentTransactionEventType eventType,
        PaymentTransactionEventResult result,
        string? providerPaymentId,
        string? providerInvoiceId,
        string? notes,
        PaymentReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        var req = new PaymentTransactionEventRequest
        {
            PaymentId = paymentId,
            PaymentProvider = MoyasarPaymentGateway.Name,
            Source = request.IsScheduled
                ? PaymentTransactionEventSource.ScheduledReconciliation
                : PaymentTransactionEventSource.ManualReconciliation,
            EventType = eventType,
            Result = result,
            ProviderPaymentId = providerPaymentId,
            ProviderInvoiceId = providerInvoiceId,
            Notes = notes
        };

        if (paymentId.HasValue)
        {
            var payment = await _payments.GetByIdWithItemsAsync(paymentId.Value, cancellationToken);
            if (payment != null)
                await _events.EnrichLinksFromPaymentAsync(req, payment, cancellationToken);
        }

        await _events.RecordAsync(req, cancellationToken);
    }
}
