using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminTeacherFinanceSummary;

public class GetAdminTeacherFinanceSummaryQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminTeacherFinanceSummaryQuery, Response<AdminTeacherFinanceSummaryDto>>
{
    private readonly IAdminFinanceService _finance;

    public GetAdminTeacherFinanceSummaryQueryHandler(
        IAdminFinanceService finance,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _finance = finance;
    }

    public async Task<Response<AdminTeacherFinanceSummaryDto>> Handle(
        GetAdminTeacherFinanceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await _finance.GetTeacherSummaryAsync(request.TeacherId, cancellationToken);
        if (summary == null)
            return NotFound<AdminTeacherFinanceSummaryDto>("Teacher not found.");

        return Success(entity: summary);
    }
}
