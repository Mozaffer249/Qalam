using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminFinanceSummary;

public class GetAdminFinanceSummaryQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminFinanceSummaryQuery, Response<AdminFinanceSummaryDto>>
{
    private readonly IAdminFinanceService _finance;

    public GetAdminFinanceSummaryQueryHandler(
        IAdminFinanceService finance,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _finance = finance;
    }

    public async Task<Response<AdminFinanceSummaryDto>> Handle(
        GetAdminFinanceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await _finance.GetSummaryAsync(
            request.FromUtc, request.ToUtc, cancellationToken);
        return Success(entity: summary);
    }
}
