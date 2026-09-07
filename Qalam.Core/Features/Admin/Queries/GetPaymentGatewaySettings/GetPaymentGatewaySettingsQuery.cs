using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Platform;

namespace Qalam.Core.Features.Admin.Queries.GetPaymentGatewaySettings;

public class GetPaymentGatewaySettingsQuery : IRequest<Response<PaymentGatewayAdminDto>>
{
}
