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
using Qalam.Service.Implementations;
using Qalam.Service.Payments;

namespace Qalam.Core.Features.Student.Payments.Commands.PayWithWallet;

/// <summary>
/// Pays a pending enrollment from the payer's wallet: debit, mark the payment paid, then activate via
/// <see cref="IPaymentConfirmationService"/>. A failed activation returns the money to the wallet
/// (schedule conflicts through the refund pipeline, anything else as a Reversal).
/// </summary>
public class PayWithWalletCommandHandler : ResponseHandler,
    IRequestHandler<PayWithWalletCommand, Response<PaymentResultDto>>
{
    private readonly IEnrollmentParticipantRepository _participantRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IStudentCoursePriceResolver _coursePriceResolver;
    private readonly IPaymentConfirmationService _confirmationService;
    private readonly IStudentWalletService _walletService;
    private readonly PaymentSettings _settings;

    public PayWithWalletCommandHandler(
        IEnrollmentParticipantRepository participantRepository,
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IStudentCoursePriceResolver coursePriceResolver,
        IPaymentConfirmationService confirmationService,
        IStudentWalletService walletService,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _participantRepository = participantRepository;
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _coursePriceResolver = coursePriceResolver;
        _confirmationService = confirmationService;
        _walletService = walletService;
        _settings = settings.Value;
    }

    public async Task<Response<PaymentResultDto>> Handle(
        PayWithWalletCommand request,
        CancellationToken cancellationToken)
    {
        var participant = await _participantRepository.GetByIdForPaymentAsync(
            request.Data?.ParticipantId ?? 0, cancellationToken);
        if (participant == null)
            return NotFound<PaymentResultDto>("Enrollment participant not found.");

        var enrollment = participant.Enrollment;
        var payabilityError = EnrollmentPayabilityRules.Validate(enrollment, request.UserId, DateTime.UtcNow);
        if (payabilityError != null)
            return BadRequest<PaymentResultDto>(payabilityError);

        var totalAmount = _coursePriceResolver.ResolveEnrollmentPayableAmount(enrollment);
        if (totalAmount <= 0)
            return BadRequest<PaymentResultDto>("Use the free-trial pay endpoint for zero-amount enrollments.");

        if (!enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Pending))
            return BadRequest<PaymentResultDto>("Enrollment has no payable participants.");

        var description = EnrollmentPayabilityRules.DescribeEnrollment(enrollment);
        var payment = new Payment
        {
            PayerUserId = request.UserId,
            Currency = _settings.DefaultCurrency,
            PaymentProvider = WalletSettings.ProviderName,
            ProviderTransactionId = "WALLET-" + Guid.NewGuid().ToString("N")[..16],
            Subtotal = totalAmount,
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

        var debit = await _walletService.DebitAsync(new WalletEntryRequest
        {
            UserId = request.UserId,
            Amount = totalAmount,
            Type = WalletTransactionType.Payment,
            PaymentId = payment.Id,
            EnrollmentId = enrollment.Id,
            Description = description,
            ReasonCode = "ENROLLMENT_PAYMENT"
        }, cancellationToken);

        if (!debit.Succeeded)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureMessage = debit.ErrorCode;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);
            return BadRequest<PaymentResultDto>(debit.ErrorCode ?? StudentWalletService.InsufficientBalance);
        }

        payment.Status = PaymentStatus.Succeeded;
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(payment);

        var outcome = await _confirmationService.ConfirmAsync(payment.Id, cancellationToken);
        if (outcome.Succeeded)
            return Success(entity: outcome.Result!);

        await ReturnUnrefundedAmountAsync(payment, enrollment.Id, outcome.ErrorCode, cancellationToken);

        if (outcome.ErrorCode is "SCHEDULE_CONFLICT_RELEASED" or "SCHEDULE_CONFLICT_REFUNDED")
            return BadRequest<PaymentResultDto>(outcome.ErrorCode);
        return BadRequest<PaymentResultDto>(outcome.ErrorMessage ?? outcome.ErrorCode ?? "Payment confirmation failed.");
    }

    private async Task ReturnUnrefundedAmountAsync(
        Payment payment,
        int enrollmentId,
        string? errorCode,
        CancellationToken cancellationToken)
    {
        var tracked = await _refundRepository.GetTrackedPaymentWithRefundsAsync(payment.Id, cancellationToken);
        var refunded = tracked?.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .Sum(r => r.Amount) ?? 0m;
        var remaining = payment.TotalAmount - refunded;
        if (remaining <= 0.001m)
            return;

        await _walletService.CreditAsync(new WalletEntryRequest
        {
            UserId = payment.PayerUserId,
            Amount = remaining,
            Type = WalletTransactionType.Reversal,
            PaymentId = payment.Id,
            EnrollmentId = enrollmentId,
            Description = "Payment could not be completed",
            ReasonCode = errorCode ?? "CONFIRM_FAILED"
        }, cancellationToken);

        var target = tracked ?? payment;
        if (target.Status != PaymentStatus.Refunded)
        {
            target.Status = PaymentStatus.Cancelled;
            target.FailureMessage = errorCode;
            target.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(target);
        }
    }
}
