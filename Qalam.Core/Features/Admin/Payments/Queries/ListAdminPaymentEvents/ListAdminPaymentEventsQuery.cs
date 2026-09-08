using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Payments.Queries.ListAdminPaymentEvents;

public class ListAdminPaymentEventsQuery : IRequest<Response<PagedResult<AdminPaymentTransactionEventDto>>>
{
    public AdminPaymentTransactionEventFilter Filter { get; set; } = new();
}

public class ListAdminPaymentEventsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminPaymentEventsQuery, Response<PagedResult<AdminPaymentTransactionEventDto>>>
{
    private readonly IAdminPaymentAuditService _audit;

    public ListAdminPaymentEventsQueryHandler(
        IAdminPaymentAuditService audit,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _audit = audit;
    }

    public async Task<Response<PagedResult<AdminPaymentTransactionEventDto>>> Handle(
        ListAdminPaymentEventsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _audit.ListEventsAsync(request.Filter, cancellationToken);
        return Success(entity: page);
    }
}
