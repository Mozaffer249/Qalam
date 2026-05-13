using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Commands.PayEnrollmentParticipant;

public class PayEnrollmentParticipantCommand : IRequest<Response<PaymentResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public PayEnrollmentParticipantRequestDto Data { get; set; } = null!;
}
