using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Platform;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetPaymentGatewaySettings;

public class GetPaymentGatewaySettingsQueryHandler : ResponseHandler,
    IRequestHandler<GetPaymentGatewaySettingsQuery, Response<PaymentGatewayAdminDto>>
{
    private readonly IPaymentGatewaySettingsProvider _provider;

    public GetPaymentGatewaySettingsQueryHandler(
        IPaymentGatewaySettingsProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<PaymentGatewayAdminDto>> Handle(
        GetPaymentGatewaySettingsQuery request,
        CancellationToken cancellationToken)
    {
        var view = await _provider.GetAdminViewAsync(cancellationToken);
        return Success(entity: view);
    }
}
