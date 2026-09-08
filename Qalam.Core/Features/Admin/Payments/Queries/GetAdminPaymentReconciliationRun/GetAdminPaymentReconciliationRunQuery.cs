using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Payments.Queries.GetAdminPaymentReconciliationRun;

public class GetAdminPaymentReconciliationRunQuery : IRequest<Response<AdminPaymentReconciliationRunDto>>
{
    public int Id { get; set; }
}

public class GetAdminPaymentReconciliationRunQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminPaymentReconciliationRunQuery, Response<AdminPaymentReconciliationRunDto>>
{
    private readonly IAdminPaymentAuditService _audit;

    public GetAdminPaymentReconciliationRunQueryHandler(
        IAdminPaymentAuditService audit,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _audit = audit;
    }

    public async Task<Response<AdminPaymentReconciliationRunDto>> Handle(
        GetAdminPaymentReconciliationRunQuery request,
        CancellationToken cancellationToken)
    {
        var run = await _audit.GetReconciliationRunAsync(request.Id, cancellationToken);
        if (run == null)
            return NotFound<AdminPaymentReconciliationRunDto>("Reconciliation run not found.");
        return Success(entity: run);
    }
}
