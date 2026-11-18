using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Commands.ConfirmIapPayment;

public class ConfirmIapPaymentCommandHandler : ResponseHandler,
    IRequestHandler<ConfirmIapPaymentCommand, Response<PaymentResultDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IAppleIapValidator _iapValidator;
    private readonly IPaymentConfirmationService _confirmationService;

    public ConfirmIapPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IAppleIapValidator iapValidator,
        IPaymentConfirmationService confirmationService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _paymentRepository = paymentRepository;
        _iapValidator = iapValidator;
        _confirmationService = confirmationService;
    }

    public async Task<Response<PaymentResultDto>> Handle(
        ConfirmIapPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var token = request.Data.TransactionToken?.Trim();
        var transactionId = request.Data.TransactionId?.Trim();

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(transactionId))
            return BadRequest<PaymentResultDto>("transactionToken and transactionId are required.");

        var payment = await _paymentRepository.GetByProviderTransactionIdAsync(token, cancellationToken);

        if (payment == null || payment.PaymentProvider != AppleIapSettings.ProviderName)
            return NotFound<PaymentResultDto>("IAP payment intent not found.");

        if (payment.PayerUserId != request.UserId)
            return BadRequest<PaymentResultDto>("Only the payer can confirm this payment.");

        if (payment.Status == PaymentStatus.Succeeded)
        {
            var existingOutcome = await _confirmationService.ConfirmAsync(payment.Id, cancellationToken);
            if (existingOutcome.Succeeded)
                return Success(entity: existingOutcome.Result!);
            return BadRequest<PaymentResultDto>(existingOutcome.ErrorMessage ?? "Confirmation failed.");
        }

        if (payment.Status != PaymentStatus.Pending)
            return BadRequest<PaymentResultDto>($"Payment is {payment.Status} — cannot confirm.");

        var validation = await _iapValidator.ValidateTransactionAsync(transactionId, cancellationToken);
        if (!validation.Succeeded)
            return BadRequest<PaymentResultDto>(validation.ErrorMessage ?? "Apple transaction validation failed.");

        payment.Status = PaymentStatus.Succeeded;
        payment.ProviderFee = 0;
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(payment);

        var outcome = await _confirmationService.ConfirmAsync(payment.Id, cancellationToken);
        if (!outcome.Succeeded)
        {
            if (outcome.ErrorCode is "SCHEDULE_CONFLICT_RELEASED" or "SCHEDULE_CONFLICT_REFUNDED")
                return BadRequest<PaymentResultDto>(outcome.ErrorCode);
            return BadRequest<PaymentResultDto>(outcome.ErrorMessage ?? "Enrollment activation failed.");
        }

        return Success(entity: outcome.Result!);
    }
}
