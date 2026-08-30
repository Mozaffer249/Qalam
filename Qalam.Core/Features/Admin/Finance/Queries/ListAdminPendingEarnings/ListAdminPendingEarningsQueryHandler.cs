using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminPendingEarnings;

public class ListAdminPendingEarningsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminPendingEarningsQuery, Response<PagedResult<AdminPendingEarningDto>>>
{
    private readonly IPayoutService _payouts;

    public ListAdminPendingEarningsQueryHandler(
        IPayoutService payouts,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _payouts = payouts;
    }

    public async Task<Response<PagedResult<AdminPendingEarningDto>>> Handle(
        ListAdminPendingEarningsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _payouts.ListPendingEarningsAsync(new AdminPendingEarningsFilter
        {
            TeacherId = request.TeacherId,
            Page = request.Page,
            PageSize = request.PageSize
        }, cancellationToken);

        return Success(entity: result);
    }
}
