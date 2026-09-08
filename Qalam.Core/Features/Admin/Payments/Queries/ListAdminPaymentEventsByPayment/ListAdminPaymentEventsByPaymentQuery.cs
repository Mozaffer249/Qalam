using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Payments.Queries.ListAdminPaymentEventsByPayment;

public class ListAdminPaymentEventsByPaymentQuery : IRequest<Response<IReadOnlyList<AdminPaymentTransactionEventDto>>>
{
    public int PaymentId { get; set; }
}

public class ListAdminPaymentEventsByPaymentQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminPaymentEventsByPaymentQuery, Response<IReadOnlyList<AdminPaymentTransactionEventDto>>>
{
    private readonly IAdminPaymentAuditService _audit;

    public ListAdminPaymentEventsByPaymentQueryHandler(
        IAdminPaymentAuditService audit,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _audit = audit;
    }

    public async Task<Response<IReadOnlyList<AdminPaymentTransactionEventDto>>> Handle(
        ListAdminPaymentEventsByPaymentQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _audit.ListEventsForPaymentAsync(request.PaymentId, cancellationToken);
        return Success(entity: items);
    }
}
