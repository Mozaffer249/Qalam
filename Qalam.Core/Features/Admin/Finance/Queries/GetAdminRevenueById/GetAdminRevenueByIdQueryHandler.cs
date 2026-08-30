using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminRevenueById;

public class GetAdminRevenueByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminRevenueByIdQuery, Response<AdminRevenueDetailDto>>
{
    private readonly IAdminFinanceService _finance;

    public GetAdminRevenueByIdQueryHandler(
        IAdminFinanceService finance,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _finance = finance;
    }

    public async Task<Response<AdminRevenueDetailDto>> Handle(
        GetAdminRevenueByIdQuery request,
        CancellationToken cancellationToken)
    {
        var detail = await _finance.GetRevenueByIdAsync(request.Id, cancellationToken);
        if (detail == null)
            return NotFound<AdminRevenueDetailDto>("Revenue record not found.");

        return Success(entity: detail);
    }
}
