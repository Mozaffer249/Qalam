using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminFinanceTransactions;

public class ListAdminFinanceTransactionsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminFinanceTransactionsQuery, Response<PagedResult<AdminFinanceTransactionDto>>>
{
    private readonly IAdminFinanceService _finance;

    public ListAdminFinanceTransactionsQueryHandler(
        IAdminFinanceService finance,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _finance = finance;
    }

    public async Task<Response<PagedResult<AdminFinanceTransactionDto>>> Handle(
        ListAdminFinanceTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _finance.ListTransactionsAsync(request.Filter, cancellationToken);
        return Success(entity: result);
    }
}
