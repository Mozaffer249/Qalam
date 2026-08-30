using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminRevenueSummary;

public class GetAdminRevenueSummaryQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminRevenueSummaryQuery, Response<AdminRevenueSummaryDto>>
{
    private readonly IAdminFinanceService _finance;

    public GetAdminRevenueSummaryQueryHandler(
        IAdminFinanceService finance,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _finance = finance;
    }

    public async Task<Response<AdminRevenueSummaryDto>> Handle(
        GetAdminRevenueSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await _finance.GetRevenueSummaryAsync(
            request.FromUtc, request.ToUtc, cancellationToken);
        return Success(entity: summary);
    }
}
