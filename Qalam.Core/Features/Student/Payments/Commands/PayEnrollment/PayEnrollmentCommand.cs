using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Commands.PayEnrollment;

public class PayEnrollmentCommand : IRequest<Response<PaymentResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public PayEnrollmentRequestDto Data { get; set; } = null!;
}
