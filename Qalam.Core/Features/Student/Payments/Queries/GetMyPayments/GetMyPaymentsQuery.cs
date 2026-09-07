using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Queries.GetMyPayments;

public class GetMyPaymentsQuery : IRequest<Response<List<StudentPaymentListItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
