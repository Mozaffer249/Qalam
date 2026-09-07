using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Commands.CreatePaymentIntent;

public class CreatePaymentIntentCommand : IRequest<Response<PaymentIntentDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public CreatePaymentIntentRequestDto Data { get; set; } = null!;
}
