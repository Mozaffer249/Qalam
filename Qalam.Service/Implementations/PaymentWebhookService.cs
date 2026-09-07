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
    private readonly ILogger<PaymentWebhookService> _logger;

    public PaymentWebhookService(
        IPaymentGatewayResolver gatewayResolver,
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IPaymentConfirmationService confirmationService,
        ILogger<PaymentWebhookService> logger)
    {
        _gatewayResolver = gatewayResolver;
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _confirmationService = confirmationService;
        _logger = logger;
    }

    public async Task<PaymentWebhookHandleResult> HandleAsync(
        string provider,
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        IPaymentGateway gateway;
        try
        {
            gateway = _gatewayResolver.Resolve(provider);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Webhook for unknown provider {Provider}", provider);
            return PaymentWebhookHandleResult.Ok("unknown_provider");
        }

        var parsed = gateway.VerifyAndParseWebhook(rawBody, headers);
        if (parsed.Auth == PaymentWebhookAuthResult.NotConfigured)
            return PaymentWebhookHandleResult.NotConfigured();
        if (parsed.Auth == PaymentWebhookAuthResult.Unauthorized)
            return PaymentWebhookHandleResult.Unauthorized();
        if (parsed.Auth == PaymentWebhookAuthResult.Ignored
            || string.IsNullOrWhiteSpace(parsed.ProviderPaymentId))
            return PaymentWebhookHandleResult.Ok(parsed.Message ?? "ignored");

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
            return PaymentWebhookHandleResult.Ok("provider_mismatch");
        }

        var normalized = (parsed.EventType ?? string.Empty).Trim().ToLowerInvariant();
        var isPaid = parsed.MappedStatus == PaymentStatus.Succeeded
            || normalized is "payment_paid" or "payment_intent.succeeded" or "checkout.session.completed"
                or "invoice_paid"
            || MoyasarStatusMapperIsPaid(normalized);

        if (isPaid)
        {
            // Confirm with the primary id (payment id when available); confirmation resolves invoice rows.
            var outcome = await _confirmationService.ConfirmFromGatewayAsync(paymentId, cancellationToken);
            if (!outcome.Succeeded
                && payment.ProviderTransactionId != null
                && !payment.ProviderTransactionId.Equals(paymentId, StringComparison.OrdinalIgnoreCase))
            {
                outcome = await _confirmationService.ConfirmFromGatewayAsync(
                    payment.ProviderTransactionId, cancellationToken);
            }

            _logger.LogInformation(
                "Webhook paid confirm: provider={Provider} paymentId={PaymentId} ok={Ok} code={Code}",
                gateway.ProviderName,
                payment.Id,
                outcome.Succeeded,
                outcome.ErrorCode);
            return PaymentWebhookHandleResult.Ok(outcome.Succeeded ? "ok" : (outcome.ErrorCode ?? "confirm_failed"));
        }

        if (parsed.MappedStatus == PaymentStatus.Failed
            || normalized is "payment_failed" or "payment_faild" or "payment_intent.payment_failed")
        {
            if (payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureMessage = Truncate(parsed.Message, 500);
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }

            return PaymentWebhookHandleResult.Ok("failed");
        }

        if (parsed.MappedStatus == PaymentStatus.Refunded
            || normalized is "payment_refunded" or "charge.refunded")
        {
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

            return PaymentWebhookHandleResult.Ok("refunded");
        }

        _logger.LogInformation(
            "Webhook unhandled type={Type} provider={Provider}",
            parsed.EventType,
            gateway.ProviderName);
        return PaymentWebhookHandleResult.Ok("ignored");
    }

    private static bool MoyasarStatusMapperIsPaid(string normalized)
        => normalized is "paid" or "captured";

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
}
