using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminTeacherFinanceTransactions;

public class ListAdminTeacherFinanceTransactionsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminTeacherFinanceTransactionsQuery, Response<PagedResult<AdminFinanceTransactionDto>>>
{
    private readonly IAdminFinanceService _finance;

    public ListAdminTeacherFinanceTransactionsQueryHandler(
        IAdminFinanceService finance,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _finance = finance;
    }

    public async Task<Response<PagedResult<AdminFinanceTransactionDto>>> Handle(
        ListAdminTeacherFinanceTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _finance.ListTeacherTransactionsAsync(
            request.TeacherId, request.Filter, cancellationToken);
        return Success(entity: result);
    }
}
