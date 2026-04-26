using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Queries.GetGroupEnrollmentPaymentSummary;

public class GetGroupEnrollmentPaymentSummaryQuery : IRequest<Response<GroupEnrollmentPaymentSummaryDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int GroupEnrollmentId { get; set; }
}
