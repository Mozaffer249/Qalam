using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Core.Features.Student.Payments.Queries.GetEnrollmentPaymentSummary;

public class GetEnrollmentPaymentSummaryQuery : IRequest<Response<EnrollmentPaymentSummaryDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int EnrollmentId { get; set; }
}
