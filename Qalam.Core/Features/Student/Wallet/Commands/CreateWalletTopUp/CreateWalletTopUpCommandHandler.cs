using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Wallet.Commands.CreateWalletTopUp;

public class CreateWalletTopUpCommandHandler : ResponseHandler,
    IRequestHandler<CreateWalletTopUpCommand, Response<PaymentIntentDto>>
{
    private readonly IStudentWalletService _walletService;
    private readonly IPaymentIntentService _intentService;

    public CreateWalletTopUpCommandHandler(
        IStudentWalletService walletService,
        IPaymentIntentService intentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _walletService = walletService;
        _intentService = intentService;
    }

    public async Task<Response<PaymentIntentDto>> Handle(
        CreateWalletTopUpCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Data == null)
            return BadRequest<PaymentIntentDto>("WALLET_TOPUP_OUT_OF_RANGE");

        var wallet = await _walletService.GetOrCreateAsync(request.UserId, cancellationToken);
        var result = await _intentService.CreateWalletTopUpAsync(
            wallet.Id,
            request.UserId,
            request.Data.Amount,
            request.Data.AppReturnUrl,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest<PaymentIntentDto>(
                result.ErrorCode is "USE_MOCK_PAY" or "WALLET_TOPUP_OUT_OF_RANGE"
                    ? result.ErrorCode
                    : result.ErrorMessage ?? "Unable to create top-up.");
        }

        return Success(entity: result.Intent!);
    }
}
