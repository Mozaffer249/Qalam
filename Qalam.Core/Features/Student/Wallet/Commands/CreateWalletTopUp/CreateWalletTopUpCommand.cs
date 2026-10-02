using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.DTOs.Wallet;

namespace Qalam.Core.Features.Student.Wallet.Commands.CreateWalletTopUp;

public class CreateWalletTopUpCommand : IRequest<Response<PaymentIntentDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public CreateWalletTopUpDto Data { get; set; } = null!;
}
