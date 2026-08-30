using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminRevenueRecords;

public class ListAdminRevenueRecordsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminRevenueRecordsQuery, Response<PagedResult<AdminRevenueRecordDto>>>
{
    private readonly IAdminFinanceService _finance;

    public ListAdminRevenueRecordsQueryHandler(
        IAdminFinanceService finance,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _finance = finance;
    }

    public async Task<Response<PagedResult<AdminRevenueRecordDto>>> Handle(
        ListAdminRevenueRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _finance.ListRevenueRecordsAsync(request.Filter, cancellationToken);
        return Success(entity: result);
    }
}
