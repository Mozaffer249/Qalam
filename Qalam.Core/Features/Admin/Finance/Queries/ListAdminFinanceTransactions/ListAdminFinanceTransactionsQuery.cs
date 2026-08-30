using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminFinanceTransactions;

public class ListAdminFinanceTransactionsQuery : IRequest<Response<PagedResult<AdminFinanceTransactionDto>>>
{
    public AdminFinanceTransactionFilter Filter { get; set; } = new();
}
