using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Platform;

namespace Qalam.Core.Features.Admin.Commands.UpdatePaymentGatewaySettings;

public class UpdatePaymentGatewaySettingsCommand : IRequest<Response<PaymentGatewayAdminDto>>
{
    public PaymentGatewaySettingsDto Settings { get; set; } = new();
}
