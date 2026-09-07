using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Payments;

namespace Qalam.Service.Implementations;

public class PaymentIntentService : IPaymentIntentService
{
    private readonly IEnrollmentParticipantRepository _participantRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStudentCoursePriceResolver _coursePriceResolver;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly IPaymentGatewaySettingsProvider _gatewaySettings;
    private readonly PaymentSettings _settings;
    private readonly ILogger<PaymentIntentService> _logger;

    public PaymentIntentService(
        IEnrollmentParticipantRepository participantRepository,
        IPaymentRepository paymentRepository,
        IStudentCoursePriceResolver coursePriceResolver,
        IPaymentGatewayResolver gatewayResolver,
        IPaymentGatewaySettingsProvider gatewaySettings,
        IOptions<PaymentSettings> settings,
        ILogger<PaymentIntentService> logger)
    {
        _participantRepository = participantRepository;
        _paymentRepository = paymentRepository;
        _coursePriceResolver = coursePriceResolver;
        _gatewayResolver = gatewayResolver;
        _gatewaySettings = gatewaySettings;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PaymentIntentServiceResult> CreateAsync(
        int participantId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        IPaymentGateway gateway;
        try
        {
            gateway = await _gatewayResolver.ResolveActiveAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return PaymentIntentServiceResult.Fail("PROVIDER_NOT_CONFIGURED", ex.Message);
        }

        if (gateway.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase))
            return PaymentIntentServiceResult.Fail(
                "USE_MOCK_PAY",
                "Active provider is Mock — use the free-trial / mock pay endpoint.");

        var participant = await _participantRepository.GetByIdForPaymentAsync(participantId, cancellationToken);
        if (participant == null)
            return PaymentIntentServiceResult.Fail("NOT_FOUND", "Enrollment participant not found.");

        var enrollment = participant.Enrollment;

        if (enrollment.EnrollmentStatus != EnrollmentStatus.PendingPayment)
            return PaymentIntentServiceResult.Fail("INVALID_STATUS", "Only pending-payment enrollments can be paid.");

        var now = DateTime.UtcNow;
        if (enrollment.PaymentDeadline.HasValue && enrollment.PaymentDeadline.Value < now)
            return PaymentIntentServiceResult.Fail("DEADLINE_EXPIRED", "Payment deadline has expired.");

        if (enrollment.EnrollmentRequest == null
            && (enrollment.SelectedSessionSlots == null || enrollment.SelectedSessionSlots.Count == 0))
        {
            return PaymentIntentServiceResult.Fail(
                "MISSING_SCHEDULE",
                "Enrollment is missing schedule selections — cannot generate schedules.");
        }

        var ownerUserId = enrollment.EnrollmentRequest?.RequestedByUserId ?? enrollment.OwnerUserId;
        if (!ownerUserId.HasValue || ownerUserId.Value != userId)
            return PaymentIntentServiceResult.Fail("NOT_OWNER", "Only the enrollment owner can pay for this enrollment.");

        if (enrollment.PaidByUserId.HasValue
            || enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded))
            return PaymentIntentServiceResult.Fail("ALREADY_PAID", "This enrollment has already been paid.");

        var totalAmount = _coursePriceResolver.ResolveEnrollmentPayableAmount(enrollment);
        if (totalAmount <= 0)
            return PaymentIntentServiceResult.Fail(
                "ZERO_AMOUNT",
                "Use the free-trial pay endpoint for zero-amount enrollments.");

        var isSessionRequest = enrollment.Source == EnrollmentSource.SessionRequest
            || enrollment.CourseId == null;

        var description = isSessionRequest
            ? (enrollment.OpenSessionRequest?.Subject?.NameEn
               ?? enrollment.OpenSessionRequest?.Subject?.NameAr
               ?? "Session request enrollment")
            : enrollment.Course?.Title ?? $"Enrollment #{enrollment.Id}";

        // Admin-controlled Moyasar mode (DB) with env fallback; other gateways use their fixed ClientMode.
        var effectiveMode = await ResolveEffectiveClientModeAsync(gateway, cancellationToken);

        Payment payment;
        string givenId;
        var reuseNative = effectiveMode == PaymentClientMode.NativeSdk
            ? await _paymentRepository.GetOpenIntentForEnrollmentAsync(
                enrollment.Id,
                gateway.ProviderName,
                cancellationToken)
            : null;

        if (reuseNative != null)
        {
            payment = reuseNative;
            givenId = string.IsNullOrWhiteSpace(payment.ProviderTransactionId)
                ? Guid.NewGuid().ToString()
                : payment.ProviderTransactionId!;

            if (payment.TotalAmount != totalAmount
                || payment.Subtotal != totalAmount
                || payment.Currency != _settings.DefaultCurrency
                || payment.PayerUserId != userId
                || string.IsNullOrWhiteSpace(payment.ProviderTransactionId))
            {
                payment.PayerUserId = userId;
                payment.Currency = _settings.DefaultCurrency;
                payment.ProviderTransactionId = givenId;
                payment.Subtotal = totalAmount;
                payment.VatAmount = 0;
                payment.DiscountAmount = 0;
                payment.TotalAmount = totalAmount;
                payment.UpdatedAt = now;
                foreach (var item in payment.PaymentItems.Where(i =>
                             i.ItemType == PaymentItemType.CourseEnrollment
                             && i.ReferenceId == enrollment.Id))
                {
                    item.Amount = totalAmount;
                    item.Description = description;
                }

                await _paymentRepository.UpdateAsync(payment);
            }
        }
        else
        {
            // Hosted: cancel prior Pending rows so finance pending sum stays accurate,
            // but keep ProviderTransactionId for late webhook matching.
            if (effectiveMode == PaymentClientMode.HostedRedirect)
            {
                await _paymentRepository.CancelOpenIntentsForEnrollmentAsync(
                    enrollment.Id,
                    gateway.ProviderName,
                    cancellationToken);
            }

            givenId = Guid.NewGuid().ToString();
            payment = new Payment
            {
                PayerUserId = userId,
                Currency = _settings.DefaultCurrency,
                PaymentProvider = gateway.ProviderName,
                ProviderTransactionId = givenId,
                Subtotal = totalAmount,
                VatAmount = 0,
                DiscountAmount = 0,
                TotalAmount = totalAmount,
                Status = PaymentStatus.Pending
            };
            payment.PaymentItems.Add(new PaymentItem
            {
                ItemType = PaymentItemType.CourseEnrollment,
                ReferenceId = enrollment.Id,
                Description = description,
                Amount = totalAmount
            });
            await _paymentRepository.AddAsync(payment);
        }

        var metadata = new Dictionary<string, string>
        {
            ["enrollmentId"] = enrollment.Id.ToString(),
            ["participantId"] = participant.Id.ToString(),
            ["paymentId"] = payment.Id.ToString()
        };

        GatewayCheckoutDto checkout;
        try
        {
            checkout = await gateway.CreateCheckoutAsync(new GatewayCheckoutRequest
            {
                GivenId = givenId,
                AmountHalalas = MinorUnitConverter.ToHalalas(payment.TotalAmount),
                Currency = payment.Currency,
                Description = description,
                Metadata = metadata,
                PreferredClientMode = effectiveMode
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateCheckout failed for provider {Provider}", gateway.ProviderName);
            payment.Status = PaymentStatus.Failed;
            payment.FailureMessage = Truncate(ex.Message, 500);
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);
            return PaymentIntentServiceResult.Fail("CHECKOUT_FAILED", ex.Message);
        }

        // Hosted providers return their own transaction ref (tran_ref / checkout id / invoice id).
        if (!string.IsNullOrWhiteSpace(checkout.ProviderPaymentRef)
            && !checkout.ProviderPaymentRef.Equals(givenId, StringComparison.Ordinal))
        {
            payment.ProviderTransactionId = checkout.ProviderPaymentRef;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);
        }

        return PaymentIntentServiceResult.Ok(new PaymentIntentDto
        {
            PaymentId = payment.Id,
            Provider = gateway.ProviderName,
            ClientMode = checkout.ClientMode,
            GivenId = payment.ProviderTransactionId ?? givenId,
            AmountHalalas = MinorUnitConverter.ToHalalas(payment.TotalAmount),
            Currency = payment.Currency,
            PublishableApiKey = checkout.PublishableKey,
            RedirectUrl = checkout.RedirectUrl,
            ClientSecret = checkout.ClientSecret,
            CallbackUrl = checkout.CallbackUrl,
            ApplePayMerchantId = checkout.ApplePayMerchantId,
            ApplePayLabel = checkout.ApplePayLabel,
            Description = description,
            Metadata = metadata
        });
    }

    private async Task<PaymentClientMode> ResolveEffectiveClientModeAsync(
        IPaymentGateway gateway,
        CancellationToken cancellationToken)
    {
        if (!gateway.ProviderName.Equals(MoyasarPaymentGateway.Name, StringComparison.OrdinalIgnoreCase))
            return gateway.ClientMode;

        var settings = await _gatewaySettings.GetSettingsAsync(cancellationToken);
        var raw = string.IsNullOrWhiteSpace(settings.MoyasarClientMode)
            ? _settings.Moyasar.ClientMode
            : settings.MoyasarClientMode;
        return PaymentGatewaySettingsDefaults.ToClientMode(raw);
    }

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
}
