using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Commands.ConfirmIapPayment;

public class ConfirmIapPaymentCommandHandler : ResponseHandler,
    IRequestHandler<ConfirmIapPaymentCommand, Response<PaymentResultDto>>
{
    private readonly IEnrollmentParticipantRepository _participantRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IAppleIapService _iap;
    private readonly IPaymentConfirmationService _confirmationService;
    private readonly PaymentSettings _paymentSettings;
    private readonly AppleIapSettings _iapSettings;

    public ConfirmIapPaymentCommandHandler(
        IEnrollmentParticipantRepository participantRepo,
        IPaymentRepository paymentRepo,
        IAppleIapService iap,
        IPaymentConfirmationService confirmationService,
        IOptions<PaymentSettings> paymentSettings,
        IOptions<AppleIapSettings> iapSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _participantRepo = participantRepo;
        _paymentRepo = paymentRepo;
        _iap = iap;
        _confirmationService = confirmationService;
        _paymentSettings = paymentSettings.Value;
        _iapSettings = iapSettings.Value;
    }

    public async Task<Response<PaymentResultDto>> Handle(
        ConfirmIapPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Data;

        // ── 1. Validate the transaction token ──────────────────────────────
        if (!_iap.TryParseTransactionToken(
                dto.TransactionToken,
                out var tokenParticipantId,
                out var tokenUserId,
                out var tokenProductId,
                out var tokenAmountSar))
        {
            return BadRequest<PaymentResultDto>("Invalid or expired transaction token.");
        }

        // Token must match the request participant and the authenticated user
        if (tokenParticipantId != dto.ParticipantId)
            return BadRequest<PaymentResultDto>("Transaction token does not match the specified participant.");
        if (tokenUserId != request.UserId)
            return BadRequest<PaymentResultDto>("Transaction token was issued for a different user.");

        // ── 2. Load participant + enrollment ───────────────────────────────
        var participant = await _participantRepo.GetByIdForPaymentAsync(dto.ParticipantId, cancellationToken);
        if (participant == null)
            return NotFound<PaymentResultDto>("Enrollment participant not found.");

        var enrollment = participant.Enrollment;

        // ── 3. Verify the Apple transaction (skipped when IAP is disabled) ──
        if (_iapSettings.Enabled)
        {
            var info = await _iap.VerifyTransactionAsync(dto.TransactionId, cancellationToken);

            if (info == null)
                return BadRequest<PaymentResultDto>(
                    "Apple transaction verification failed. Please contact support.");

            // Bundle ID must match
            if (!string.Equals(info.BundleId, _iapSettings.BundleId, StringComparison.OrdinalIgnoreCase))
                return BadRequest<PaymentResultDto>("Transaction bundle ID mismatch.");

            // Product ID must match what was issued in the intent
            if (!string.Equals(info.ProductId, tokenProductId, StringComparison.OrdinalIgnoreCase))
                return BadRequest<PaymentResultDto>("Transaction product ID mismatch.");

            // applicationUsername (passed as transactionToken to StoreKit) must round-trip
            if (!string.IsNullOrEmpty(info.ApplicationUsername) &&
                !string.Equals(info.ApplicationUsername, dto.TransactionToken, StringComparison.Ordinal))
            {
                return BadRequest<PaymentResultDto>("Transaction token mismatch in Apple receipt.");
            }

            // Reject transactions older than 24 hours (replay protection)
            if (info.PurchaseDateMs > 0)
            {
                var purchaseDate = DateTimeOffset.FromUnixTimeMilliseconds(info.PurchaseDateMs);
                if (DateTime.UtcNow - purchaseDate.UtcDateTime > TimeSpan.FromHours(24))
                    return BadRequest<PaymentResultDto>("Transaction is too old. Please contact support.");
            }
        }

        // ── 4. Create a succeeded Payment record ───────────────────────────
        var isSessionRequest = enrollment.Source == EnrollmentSource.SessionRequest
            || enrollment.CourseId == null;

        var payment = new Payment
        {
            PayerUserId = request.UserId,
            Currency = _paymentSettings.DefaultCurrency,
            PaymentProvider = "AppleIAP",
            ProviderTransactionId = dto.TransactionId,
            Subtotal = tokenAmountSar,
            VatAmount = 0,
            DiscountAmount = 0,
            TotalAmount = tokenAmountSar,
            Status = PaymentStatus.Succeeded
        };

        payment.PaymentItems.Add(new PaymentItem
        {
            ItemType = PaymentItemType.CourseEnrollment,
            ReferenceId = enrollment.Id,
            Description = isSessionRequest
                ? (enrollment.OpenSessionRequest?.Subject?.NameEn
                   ?? enrollment.OpenSessionRequest?.Subject?.NameAr
                   ?? "Session request enrollment")
                : enrollment.Course?.Title,
            Amount = tokenAmountSar
        });

        await _paymentRepo.AddAsync(payment);

        // ── 5. Activate enrollment ─────────────────────────────────────────
        var outcome = await _confirmationService.ConfirmAsync(payment.Id, cancellationToken);
        if (!outcome.Succeeded)
        {
            if (outcome.ErrorCode is "SCHEDULE_CONFLICT_RELEASED" or "SCHEDULE_CONFLICT_REFUNDED")
                return BadRequest<PaymentResultDto>(outcome.ErrorCode);
            return BadRequest<PaymentResultDto>(outcome.ErrorMessage ?? "Payment confirmation failed.");
        }

        return Success(entity: outcome.Result!);
    }
}
