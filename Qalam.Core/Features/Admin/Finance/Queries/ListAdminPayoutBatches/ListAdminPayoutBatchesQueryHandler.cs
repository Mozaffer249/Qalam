using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminPayoutBatches;

public class ListAdminPayoutBatchesQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminPayoutBatchesQuery, Response<PagedResult<AdminPayoutBatchListItemDto>>>
{
    private readonly IPayoutService _payouts;

    public ListAdminPayoutBatchesQueryHandler(
        IPayoutService payouts,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _payouts = payouts;
    }

    public async Task<Response<PagedResult<AdminPayoutBatchListItemDto>>> Handle(
        ListAdminPayoutBatchesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _payouts.ListBatchesAsync(new AdminPayoutListFilter
        {
            Status = request.Status,
            TeacherId = request.TeacherId,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Page = request.Page,
            PageSize = request.PageSize
        }, cancellationToken);

        return Success(entity: result);
    }
}
