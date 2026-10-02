using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.DTOs.Wallet;

namespace Qalam.Core.Features.Student.Payments.Commands.PayWithWallet;

public class PayWithWalletCommand : IRequest<Response<PaymentResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public PayWithWalletDto Data { get; set; } = null!;
}
