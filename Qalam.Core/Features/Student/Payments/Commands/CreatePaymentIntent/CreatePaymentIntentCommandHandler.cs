using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Commands.CreatePaymentIntent;

public class CreatePaymentIntentCommandHandler : ResponseHandler,
    IRequestHandler<CreatePaymentIntentCommand, Response<PaymentIntentDto>>
{
    private readonly IPaymentIntentService _intentService;

    public CreatePaymentIntentCommandHandler(
        IPaymentIntentService intentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _intentService = intentService;
    }

    public async Task<Response<PaymentIntentDto>> Handle(
        CreatePaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _intentService.CreateAsync(
            request.Data.ParticipantId,
            request.UserId,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound<PaymentIntentDto>(result.ErrorMessage ?? "Not found.");
            return BadRequest<PaymentIntentDto>(result.ErrorMessage ?? "Unable to create payment intent.");
        }

        return Success(entity: result.Intent!);
    }
}
