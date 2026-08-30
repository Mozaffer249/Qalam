using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminPayoutBatchById;

public class GetAdminPayoutBatchByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminPayoutBatchByIdQuery, Response<AdminPayoutBatchDto>>
{
    private readonly IPayoutService _payouts;

    public GetAdminPayoutBatchByIdQueryHandler(
        IPayoutService payouts,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _payouts = payouts;
    }

    public async Task<Response<AdminPayoutBatchDto>> Handle(
        GetAdminPayoutBatchByIdQuery request,
        CancellationToken cancellationToken)
    {
        var batch = await _payouts.GetBatchAsync(request.Id, cancellationToken);
        if (batch == null)
            return NotFound<AdminPayoutBatchDto>("Payout batch not found.");

        return Success(entity: batch);
    }
}
