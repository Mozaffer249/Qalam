using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.ListAdminRefunds;

public class ListAdminRefundsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminRefundsQuery, Response<PagedResult<AdminRefundListItemDto>>>
{
    private readonly IRefundService _refunds;

    public ListAdminRefundsQueryHandler(
        IRefundService refunds,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _refunds = refunds;
    }

    public async Task<Response<PagedResult<AdminRefundListItemDto>>> Handle(
        ListAdminRefundsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _refunds.ListAsync(new AdminRefundListFilter
        {
            Status = request.Status,
            EnrollmentId = request.EnrollmentId,
            TeacherId = request.TeacherId,
            StudentId = request.StudentId,
            Search = request.Search,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Page = request.Page,
            PageSize = request.PageSize
        }, cancellationToken);

        return Success(entity: result);
    }
}
