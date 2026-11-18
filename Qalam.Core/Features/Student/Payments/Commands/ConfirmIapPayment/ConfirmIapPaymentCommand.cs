using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Commands.ConfirmIapPayment;

public class ConfirmIapPaymentCommand : IRequest<Response<PaymentResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public ConfirmIapPaymentRequestDto Data { get; set; } = null!;
}
