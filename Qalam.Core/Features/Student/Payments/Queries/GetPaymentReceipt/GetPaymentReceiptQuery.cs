using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Queries.GetPaymentReceipt;

public class GetPaymentReceiptQuery : IRequest<Response<StudentPaymentReceiptDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int PaymentId { get; set; }
}
