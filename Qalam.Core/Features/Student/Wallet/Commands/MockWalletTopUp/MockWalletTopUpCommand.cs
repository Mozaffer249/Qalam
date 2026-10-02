using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.DTOs.Wallet;

namespace Qalam.Core.Features.Student.Wallet.Commands.MockWalletTopUp;

/// <summary>Dev/staging top-up when the active gateway is Mock (no card checkout).</summary>
public class MockWalletTopUpCommand : IRequest<Response<PaymentResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public CreateWalletTopUpDto Data { get; set; } = null!;
}
