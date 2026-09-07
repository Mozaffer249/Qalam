using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Helpers;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Platform;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.UpdatePaymentGatewaySettings;

public class UpdatePaymentGatewaySettingsCommandHandler : ResponseHandler,
    IRequestHandler<UpdatePaymentGatewaySettingsCommand, Response<PaymentGatewayAdminDto>>
{
    private readonly IPaymentGatewaySettingsProvider _provider;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdatePaymentGatewaySettingsCommandHandler(
        IPaymentGatewaySettingsProvider provider,
        IAuditService audit,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<PaymentGatewayAdminDto>> Handle(
        UpdatePaymentGatewaySettingsCommand request,
        CancellationToken cancellationToken)
    {
        var previous = await _provider.GetSettingsAsync(cancellationToken);
        var saved = await _provider.SaveSettingsAsync(request.Settings, cancellationToken);

        await _audit.LogAsync(
            "Payments.GatewayChanged",
            userId: null,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            userAgent: ClientIpHelper.GetUserAgent(_httpContextAccessor.HttpContext),
            details: $"Active payment gateway changed from '{previous.ActiveProvider}' to '{saved.ActiveProvider}'",
            entityType: "SystemSetting",
            entityId: "Payments.Gateway");

        var view = await _provider.GetAdminViewAsync(cancellationToken);
        return Success("Payment gateway settings updated successfully", entity: view);
    }
}
