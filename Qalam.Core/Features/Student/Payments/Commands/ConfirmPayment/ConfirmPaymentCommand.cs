using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Commands.ConfirmPayment;

public class ConfirmPaymentCommand : IRequest<Response<PaymentResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public ConfirmPaymentRequestDto Data { get; set; } = null!;
}
