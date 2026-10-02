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

namespace Qalam.Core.Features.Student.Wallet.Commands.MockWalletTopUp;

public class MockWalletTopUpCommandHandler : ResponseHandler,
    IRequestHandler<MockWalletTopUpCommand, Response<PaymentResultDto>>
{
    private readonly IStudentWalletService _walletService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentConfirmationService _confirmationService;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly PaymentSettings _settings;

    public MockWalletTopUpCommandHandler(
        IStudentWalletService walletService,
        IPaymentRepository paymentRepository,
        IPaymentConfirmationService confirmationService,
        IPaymentGatewayResolver gatewayResolver,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _walletService = walletService;
        _paymentRepository = paymentRepository;
        _confirmationService = confirmationService;
        _gatewayResolver = gatewayResolver;
        _settings = settings.Value;
    }

    public async Task<Response<PaymentResultDto>> Handle(
        MockWalletTopUpCommand request,
        CancellationToken cancellationToken)
    {
        var amount = Math.Round(request.Data?.Amount ?? 0, 2, MidpointRounding.AwayFromZero);
        if (amount < _settings.Wallet.MinTopUp || amount > _settings.Wallet.MaxTopUp)
            return BadRequest<PaymentResultDto>("WALLET_TOPUP_OUT_OF_RANGE");

        var active = await _gatewayResolver.ResolveActiveAsync(cancellationToken);
        if (!active.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase))
            return BadRequest<PaymentResultDto>("Use wallet top-up checkout for card payments.");

        var wallet = await _walletService.GetOrCreateAsync(request.UserId, cancellationToken);
        var payment = new Payment
        {
            PayerUserId = request.UserId,
            Currency = _settings.DefaultCurrency,
            PaymentProvider = _settings.MockProviderName,
            ProviderTransactionId = "MOCK-" + Guid.NewGuid().ToString("N")[..16],
            Subtotal = amount,
            TotalAmount = amount,
            Status = PaymentStatus.Succeeded
        };
        payment.PaymentItems.Add(new PaymentItem
        {
            ItemType = PaymentItemType.WalletTopUp,
            ReferenceId = wallet.Id,
            Description = "Qalam wallet top-up",
            Amount = amount
        });
        await _paymentRepository.AddAsync(payment);

        var outcome = await _confirmationService.ConfirmAsync(payment.Id, cancellationToken);
        return outcome.Succeeded
            ? Success(entity: outcome.Result!)
            : BadRequest<PaymentResultDto>(outcome.ErrorCode ?? "Top-up failed.");
    }
}
