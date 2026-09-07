using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Commands.ConfirmPayment;

public class ConfirmPaymentCommandHandler : ResponseHandler,
    IRequestHandler<ConfirmPaymentCommand, Response<PaymentResultDto>>
{
    private readonly IPaymentConfirmationService _confirmationService;

    public ConfirmPaymentCommandHandler(
        IPaymentConfirmationService confirmationService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _confirmationService = confirmationService;
    }

    public async Task<Response<PaymentResultDto>> Handle(
        ConfirmPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var givenId = request.Data.GivenId?.Trim();
        if (string.IsNullOrWhiteSpace(givenId))
            return BadRequest<PaymentResultDto>("givenId is required.");

        // Full invoice↔payment resolution (not raw ProviderTransactionId equality).
        var ownership = await _confirmationService.ResolveLocalPaymentAsync(givenId, cancellationToken);
        if (ownership == null)
            return NotFound<PaymentResultDto>("Payment intent not found.");

        if (ownership.PayerUserId != request.UserId)
            return BadRequest<PaymentResultDto>("Only the payer can confirm this payment.");

        var outcome = await _confirmationService.ConfirmFromGatewayAsync(givenId, cancellationToken);
        if (!outcome.Succeeded)
        {
            if (outcome.ErrorCode == "PAYMENT_NOT_FOUND")
                return NotFound<PaymentResultDto>(outcome.ErrorMessage ?? "Payment intent not found.");
            if (outcome.ErrorCode is "SCHEDULE_CONFLICT_RELEASED" or "SCHEDULE_CONFLICT_REFUNDED")
                return BadRequest<PaymentResultDto>(outcome.ErrorCode);
            return BadRequest<PaymentResultDto>(outcome.ErrorMessage ?? "Payment confirmation failed.");
        }

        return Success(entity: outcome.Result!);
    }
}
