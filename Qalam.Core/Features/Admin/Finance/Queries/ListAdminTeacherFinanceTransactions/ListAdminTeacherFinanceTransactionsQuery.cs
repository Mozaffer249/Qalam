using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminTeacherFinanceTransactions;

public class ListAdminTeacherFinanceTransactionsQuery : IRequest<Response<PagedResult<AdminFinanceTransactionDto>>>
{
    public int TeacherId { get; set; }
    public AdminFinanceTransactionFilter Filter { get; set; } = new();
}
