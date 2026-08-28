using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceTransactionDetail;

public class GetFinanceTransactionDetailQuery : IRequest<Response<TeacherFinanceTransactionDetailDto>>, IAuthenticatedRequest
{
    public string TransactionId { get; set; } = default!;

    [BindNever]
    public int UserId { get; set; }
}
