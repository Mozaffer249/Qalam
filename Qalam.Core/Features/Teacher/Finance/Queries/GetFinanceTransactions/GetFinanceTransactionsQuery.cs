using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceTransactions;

public class GetFinanceTransactionsQuery : IRequest<Response<PaginatedResult<TeacherFinanceTransactionDto>>>, IAuthenticatedRequest
{
    public string? Filter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    [BindNever]
    public int UserId { get; set; }
}
