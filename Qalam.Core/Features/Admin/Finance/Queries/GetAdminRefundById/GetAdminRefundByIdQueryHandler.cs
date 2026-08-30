using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminRefundById;

public class GetAdminRefundByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminRefundByIdQuery, Response<AdminRefundDetailDto>>
{
    private readonly IRefundService _refunds;

    public GetAdminRefundByIdQueryHandler(
        IRefundService refunds,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _refunds = refunds;
    }

    public async Task<Response<AdminRefundDetailDto>> Handle(
        GetAdminRefundByIdQuery request,
        CancellationToken cancellationToken)
    {
        var detail = await _refunds.GetByIdAsync(request.Id, cancellationToken);
        if (detail == null)
            return NotFound<AdminRefundDetailDto>("Refund not found.");

        return Success(entity: detail);
    }
}
