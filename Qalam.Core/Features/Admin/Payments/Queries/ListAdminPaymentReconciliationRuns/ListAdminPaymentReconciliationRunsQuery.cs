using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Payments.Queries.ListAdminPaymentReconciliationRuns;

public class ListAdminPaymentReconciliationRunsQuery : IRequest<Response<PagedResult<AdminPaymentReconciliationRunDto>>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class ListAdminPaymentReconciliationRunsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminPaymentReconciliationRunsQuery, Response<PagedResult<AdminPaymentReconciliationRunDto>>>
{
    private readonly IAdminPaymentAuditService _audit;

    public ListAdminPaymentReconciliationRunsQueryHandler(
        IAdminPaymentAuditService audit,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _audit = audit;
    }

    public async Task<Response<PagedResult<AdminPaymentReconciliationRunDto>>> Handle(
        ListAdminPaymentReconciliationRunsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _audit.ListReconciliationRunsAsync(request.Page, request.PageSize, cancellationToken);
        return Success(entity: page);
    }
}
