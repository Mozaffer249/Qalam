using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PaymentWebhookService : IPaymentWebhookService
{
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IPaymentConfirmationService _confirmationService;
    private readonly IPaymentTransactionEventService _events;
    private readonly ILogger<PaymentWebhookService> _logger;

    public PaymentWebhookService(
        IPaymentGatewayResolver gatewayResolver,
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IPaymentConfirmationService confirmationService,
        IPaymentTransactionEventService events,
        ILogger<PaymentWebhookService> logger)
    {
        _gatewayResolver = gatewayResolver;
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _confirmationService = confirmationService;
        _events = events;
        _logger = logger;
    }

    public async Task<PaymentWebhookHandleResult> HandleAsync(
        string provider,
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        var receivedAt = DateTime.UtcNow;
        var payloadHash = _events.ComputePayloadHash(rawBody);
        var correlationId = Guid.NewGuid().ToString("N");

        IPaymentGateway gateway;
        try
        {
            gateway = _gatewayResolver.Resolve(provider);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Webhook for unknown provider {Provider}", provider);
            await _events.RecordAsync(new PaymentTransactionEventRequest
            {
                PaymentProvider = provider,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.WebhookUnmatched,
                Result = PaymentTransactionEventResult.Ignored,
                PayloadHash = payloadHash,
                RawPayload = rawBody,
                CorrelationId = correlationId,
                Notes = "unknown_provider",
                ReceivedAt = receivedAt
            }, cancellationToken);
            return PaymentWebhookHandleResult.Ok("unknown_provider");
        }

        var parsed = gateway.VerifyAndParseWebhook(rawBody, headers);
        var providerEventId = ExtractProviderEventId(headers, parsed);

        // Detect retries before writing the delivery row so hash/event-id self-matches are avoided.
        if (parsed.Auth == PaymentWebhookAuthResult.Ok
            && await _events.IsDuplicateDeliveryAsync(
                gateway.ProviderName, providerEventId, payloadHash, cancellationToken))
        {
            await _events.RecordAsync(new PaymentTransactionEventRequest
            {
                PaymentProvider = gateway.ProviderName,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.WebhookDuplicate,
                Result = PaymentTransactionEventResult.Ignored,
                ProviderPaymentId = parsed.ProviderPaymentId,
                ProviderEventId = providerEventId,
                PayloadHash = payloadHash,
                CorrelationId = correlationId,
                Notes = "duplicate_delivery",
                ReceivedAt = receivedAt
            }, cancellationToken);
            return PaymentWebhookHandleResult.Ok("duplicate");
        }

        await _events.RecordAsync(new PaymentTransactionEventRequest
        {
            PaymentProvider = gateway.ProviderName,
            Source = PaymentTransactionEventSource.Webhook,
            EventType = PaymentTransactionEventType.WebhookReceived,
            Result = PaymentTransactionEventResult.Pending,
            ProviderPaymentId = parsed.ProviderPaymentId,
            ProviderInvoiceId = parsed.AlternateProviderPaymentIds?.FirstOrDefault(),
            ProviderEventId = providerEventId,
            PayloadHash = payloadHash,
            RawPayload = rawBody,
            CorrelationId = correlationId,
            Notes = parsed.EventType,
            ReceivedAt = receivedAt
        }, cancellationToken);

        if (parsed.Auth == PaymentWebhookAuthResult.NotConfigured)
            return PaymentWebhookHandleResult.NotConfigured();

        if (parsed.Auth == PaymentWebhookAuthResult.Unauthorized)
        {
            await _events.RecordAsync(new PaymentTransactionEventRequest
            {
                PaymentProvider = gateway.ProviderName,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.WebhookAuthRejected,
                Result = PaymentTransactionEventResult.Unauthorized,
                PayloadHash = payloadHash,
                RawPayload = rawBody,
                CorrelationId = correlationId,
                ReceivedAt = receivedAt
            }, cancellationToken);
            return PaymentWebhookHandleResult.Unauthorized();
        }

        if (parsed.Auth == PaymentWebhookAuthResult.Ignored
            || string.IsNullOrWhiteSpace(parsed.ProviderPaymentId))
        {
            await _events.RecordAsync(new PaymentTransactionEventRequest
            {
                PaymentProvider = gateway.ProviderName,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.WebhookUnmatched,
                Result = PaymentTransactionEventResult.Ignored,
                PayloadHash = payloadHash,
                RawPayload = rawBody,
                CorrelationId = correlationId,
                Notes = parsed.Message ?? "ignored",
                ReceivedAt = receivedAt
            }, cancellationToken);
            return PaymentWebhookHandleResult.Ok(parsed.Message ?? "ignored");
        }

        var paymentId = parsed.ProviderPaymentId!;
        var payment = await _paymentRepository.GetByProviderTransactionIdAsync(paymentId, cancellationToken);
        if (payment == null && parsed.AlternateProviderPaymentIds != null)
        {
            foreach (var alt in parsed.AlternateProviderPaymentIds)
            {
                if (string.IsNullOrWhiteSpace(alt)) continue;
                payment = await _paymentRepository.GetByProviderTransactionIdAsync(alt, cancellationToken);
                if (payment != null) break;
            }
        }

        if (payment == null)
        {
            _logger.LogInformation(
                "Webhook {Provider}: unknown local payment {Id}",
                gateway.ProviderName,
                paymentId);
            await _events.RecordAsync(new PaymentTransactionEventRequest
            {
                PaymentProvider = gateway.ProviderName,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.WebhookUnmatched,
                Result = PaymentTransactionEventResult.NotFound,
                ProviderPaymentId = paymentId,
                ProviderInvoiceId = parsed.AlternateProviderPaymentIds?.FirstOrDefault(),
                ProviderEventId = providerEventId,
                PayloadHash = payloadHash,
                RawPayload = rawBody,
                CorrelationId = correlationId,
                Notes = "unknown_payment",
                ReceivedAt = receivedAt
            }, cancellationToken);
            return PaymentWebhookHandleResult.Ok("unknown_payment");
        }

        if (!payment.PaymentProvider.Equals(gateway.ProviderName, StringComparison.OrdinalIgnoreCase)
            && !(gateway.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase)
                 && payment.PaymentProvider.Equals("MOCK", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning(
                "Webhook provider mismatch: route={Route} payment={PaymentProvider} id={Id}",
                gateway.ProviderName,
                payment.PaymentProvider,
                paymentId);

            var mismatchReq = new PaymentTransactionEventRequest
            {
                PaymentProvider = gateway.ProviderName,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.WebhookProviderMismatch,
                Result = PaymentTransactionEventResult.Mismatch,
                ProviderPaymentId = paymentId,
                ProviderEventId = providerEventId,
                PayloadHash = payloadHash,
                CorrelationId = correlationId,
                Notes = $"route={gateway.ProviderName};payment={payment.PaymentProvider}",
                ReceivedAt = receivedAt
            };
            await _events.EnrichLinksFromPaymentAsync(mismatchReq, payment, cancellationToken);
            await _events.RecordAsync(mismatchReq, cancellationToken);
            return PaymentWebhookHandleResult.Ok("provider_mismatch");
        }

        var mappedStatus = parsed.MappedStatus;
        var eventType = parsed.EventType;
        if (parsed.RequiresRemoteVerification)
        {
            var remote = await gateway.FetchAsync(paymentId, cancellationToken)
                ?? (payment.ProviderInvoiceId != null
                    ? await gateway.FetchAsync(payment.ProviderInvoiceId, cancellationToken)
                    : null)
                ?? (payment.ProviderTransactionId != null
                    ? await gateway.FetchAsync(payment.ProviderTransactionId, cancellationToken)
                    : null);

            if (remote == null)
            {
                var failReq = new PaymentTransactionEventRequest
                {
                    PaymentProvider = gateway.ProviderName,
                    Source = PaymentTransactionEventSource.Webhook,
                    EventType = PaymentTransactionEventType.WebhookProcessed,
                    Result = PaymentTransactionEventResult.Failed,
                    StatusBefore = payment.Status,
                    ProviderPaymentId = paymentId,
                    ProviderInvoiceId = payment.ProviderInvoiceId,
                    ProviderEventId = providerEventId,
                    PayloadHash = payloadHash,
                    CorrelationId = correlationId,
                    ErrorMessage = "remote_verification_failed",
                    ReceivedAt = receivedAt
                };
                await _events.EnrichLinksFromPaymentAsync(failReq, payment, cancellationToken);
                await _events.RecordAsync(failReq, cancellationToken);
                return PaymentWebhookHandleResult.Ok("remote_verification_failed");
            }

            mappedStatus = remote.MappedStatus;
            eventType ??= "remote_" + (remote.Status ?? "verified");
            if (!string.IsNullOrWhiteSpace(remote.InvoiceId) && string.IsNullOrWhiteSpace(payment.ProviderInvoiceId))
            {
                payment.ProviderInvoiceId = remote.InvoiceId;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }
        }

        var normalized = (eventType ?? string.Empty).Trim().ToLowerInvariant();
        var isPaid = mappedStatus == PaymentStatus.Succeeded
            || normalized is "payment_paid" or "payment_intent.succeeded" or "checkout.session.completed"
                or "invoice_paid"
            || MoyasarStatusMapperIsPaid(normalized);

        if (isPaid)
        {
            var statusBefore = payment.Status;
            var outcome = await _confirmationService.ConfirmFromGatewayAsync(paymentId, cancellationToken);
            if (!outcome.Succeeded
                && payment.ProviderTransactionId != null
                && !payment.ProviderTransactionId.Equals(paymentId, StringComparison.OrdinalIgnoreCase))
            {
                outcome = await _confirmationService.ConfirmFromGatewayAsync(
                    payment.ProviderTransactionId, cancellationToken);
            }

            var paidReq = new PaymentTransactionEventRequest
            {
                PaymentProvider = gateway.ProviderName,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.WebhookProcessed,
                Result = outcome.Succeeded
                    ? PaymentTransactionEventResult.Success
                    : PaymentTransactionEventResult.Failed,
                StatusBefore = statusBefore,
                StatusAfter = outcome.Succeeded ? PaymentStatus.Succeeded : payment.Status,
                ProviderPaymentId = paymentId,
                ProviderInvoiceId = payment.ProviderInvoiceId,
                ProviderEventId = providerEventId,
                PayloadHash = payloadHash,
                CorrelationId = correlationId,
                ErrorMessage = outcome.Succeeded ? null : outcome.ErrorCode,
                Notes = outcome.Succeeded ? "ok" : (outcome.ErrorCode ?? "confirm_failed"),
                ReceivedAt = receivedAt
            };
            await _events.EnrichLinksFromPaymentAsync(paidReq, payment, cancellationToken);
            await _events.RecordAsync(paidReq, cancellationToken);

            _logger.LogInformation(
                "Webhook paid confirm: provider={Provider} paymentId={PaymentId} ok={Ok} code={Code}",
                gateway.ProviderName,
                payment.Id,
                outcome.Succeeded,
                outcome.ErrorCode);
            return PaymentWebhookHandleResult.Ok(outcome.Succeeded ? "ok" : (outcome.ErrorCode ?? "confirm_failed"));
        }

        if (mappedStatus == PaymentStatus.Failed
            || normalized is "payment_failed" or "payment_faild" or "payment_intent.payment_failed"
            || normalized.StartsWith("invoice_failed", StringComparison.Ordinal)
            || normalized is "invoice_expired" or "invoice_canceled" or "invoice_voided")
        {
            var statusBefore = payment.Status;
            if (payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureMessage = Truncate(parsed.Message, 500);
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }

            var failedReq = new PaymentTransactionEventRequest
            {
                PaymentProvider = gateway.ProviderName,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.StatusChanged,
                Result = PaymentTransactionEventResult.Success,
                StatusBefore = statusBefore,
                StatusAfter = payment.Status,
                ProviderPaymentId = paymentId,
                ProviderInvoiceId = payment.ProviderInvoiceId,
                ProviderEventId = providerEventId,
                PayloadHash = payloadHash,
                CorrelationId = correlationId,
                Notes = "failed",
                ReceivedAt = receivedAt
            };
            await _events.EnrichLinksFromPaymentAsync(failedReq, payment, cancellationToken);
            await _events.RecordAsync(failedReq, cancellationToken);
            return PaymentWebhookHandleResult.Ok("failed");
        }

        if (mappedStatus == PaymentStatus.Refunded
            || normalized is "payment_refunded" or "charge.refunded" or "invoice_refunded")
        {
            var statusBefore = payment.Status;
            if (payment.Status != PaymentStatus.Refunded)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                var eps = await _refundRepository.GetEnrollmentPaymentsForPaymentAsync(
                    payment.Id, cancellationToken);
                foreach (var ep in eps)
                    ep.Status = PaymentStatus.Refunded;
                await _refundRepository.SaveChangesAsync(cancellationToken);
            }

            var refundReq = new PaymentTransactionEventRequest
            {
                PaymentProvider = gateway.ProviderName,
                Source = PaymentTransactionEventSource.Webhook,
                EventType = PaymentTransactionEventType.RefundSucceeded,
                Result = PaymentTransactionEventResult.Success,
                StatusBefore = statusBefore,
                StatusAfter = PaymentStatus.Refunded,
                ProviderPaymentId = paymentId,
                ProviderInvoiceId = payment.ProviderInvoiceId,
                ProviderEventId = providerEventId,
                PayloadHash = payloadHash,
                CorrelationId = correlationId,
                Notes = "refunded",
                ReceivedAt = receivedAt
            };
            await _events.EnrichLinksFromPaymentAsync(refundReq, payment, cancellationToken);
            await _events.RecordAsync(refundReq, cancellationToken);
            return PaymentWebhookHandleResult.Ok("refunded");
        }

        _logger.LogInformation(
            "Webhook unhandled type={Type} provider={Provider}",
            eventType,
            gateway.ProviderName);

        var ignoredReq = new PaymentTransactionEventRequest
        {
            PaymentProvider = gateway.ProviderName,
            Source = PaymentTransactionEventSource.Webhook,
            EventType = PaymentTransactionEventType.WebhookProcessed,
            Result = PaymentTransactionEventResult.Ignored,
            ProviderPaymentId = paymentId,
            ProviderEventId = providerEventId,
            PayloadHash = payloadHash,
            CorrelationId = correlationId,
            Notes = eventType ?? "ignored",
            ReceivedAt = receivedAt
        };
        await _events.EnrichLinksFromPaymentAsync(ignoredReq, payment, cancellationToken);
        await _events.RecordAsync(ignoredReq, cancellationToken);
        return PaymentWebhookHandleResult.Ok("ignored");
    }

    private static string? ExtractProviderEventId(
        IReadOnlyDictionary<string, string> headers,
        PaymentWebhookParseResult parsed)
    {
        foreach (var key in new[] { "X-Moyasar-Delivery-Id", "X-Request-Id", "X-Event-Id" })
        {
            if (headers.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        if (!string.IsNullOrWhiteSpace(parsed.EventType) && !string.IsNullOrWhiteSpace(parsed.ProviderPaymentId))
            return $"{parsed.EventType}:{parsed.ProviderPaymentId}";

        return null;
    }

    private static bool MoyasarStatusMapperIsPaid(string normalized)
        => normalized is "paid" or "captured";

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
}
