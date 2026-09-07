using Qalam.Data.DTOs.Platform;

namespace Qalam.Service.Abstracts;

public interface IPaymentGatewaySettingsProvider
{
    Task<PaymentGatewaySettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<PaymentGatewaySettingsDto> SaveSettingsAsync(
        PaymentGatewaySettingsDto settings,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayAdminDto> GetAdminViewAsync(CancellationToken cancellationToken = default);
}
