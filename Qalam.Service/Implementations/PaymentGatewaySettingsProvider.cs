using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Entity.Common;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PaymentGatewaySettingsProvider : IPaymentGatewaySettingsProvider
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly PaymentSettings _envSettings;
    private readonly ILogger<PaymentGatewaySettingsProvider> _logger;

    public PaymentGatewaySettingsProvider(
        ISystemSettingRepository systemSettingRepository,
        IEnumerable<IPaymentGateway> gateways,
        IOptions<PaymentSettings> envSettings,
        ILogger<PaymentGatewaySettingsProvider> logger)
    {
        _systemSettingRepository = systemSettingRepository;
        _gateways = gateways;
        _envSettings = envSettings.Value;
        _logger = logger;
    }

    public async Task<PaymentGatewaySettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var row = await _systemSettingRepository.GetByKeyAsync(
            PaymentGatewaySettingsKeys.SettingsKey,
            cancellationToken);
        if (row != null)
        {
            try
            {
                var dto = PaymentGatewaySettingsDefaults.FromJson(row.Value);
                // Older rows may lack MoyasarClientMode — fill from env.
                if (string.IsNullOrWhiteSpace(dto.MoyasarClientMode))
                {
                    dto.MoyasarClientMode = PaymentGatewaySettingsDefaults.NormalizeMoyasarClientMode(
                        _envSettings.Moyasar.ClientMode);
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Payments.Gateway JSON; using env fallback");
            }
        }

        return PaymentGatewaySettingsDefaults.Create(
            _envSettings.Provider,
            _envSettings.Moyasar.ClientMode);
    }

    public async Task<PaymentGatewaySettingsDto> SaveSettingsAsync(
        PaymentGatewaySettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        settings.ActiveProvider = settings.ActiveProvider?.Trim() ?? "Mock";
        settings.MoyasarClientMode =
            PaymentGatewaySettingsDefaults.NormalizeMoyasarClientMode(settings.MoyasarClientMode);

        await _systemSettingRepository.UpsertAsync(new SystemSetting
        {
            Key = PaymentGatewaySettingsKeys.SettingsKey,
            Value = PaymentGatewaySettingsDefaults.ToJson(settings),
            Type = SettingType.JSON,
            IsPublic = false,
            DescriptionEn = "Active payment gateway and Moyasar client mode for new payment intents",
            DescriptionAr = "بوابة الدفع النشطة ووضع عرض ميسر لعمليات الدفع الجديدة"
        }, cancellationToken);

        return settings;
    }

    public async Task<PaymentGatewayAdminDto> GetAdminViewAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var moyasarMode = PaymentGatewaySettingsDefaults.NormalizeMoyasarClientMode(settings.MoyasarClientMode);

        return new PaymentGatewayAdminDto
        {
            ActiveProvider = settings.ActiveProvider,
            MoyasarClientMode = moyasarMode,
            Providers = _gateways
                .DistinctBy(g => g.ProviderName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var mode = g.ClientMode.ToString();
                    // Reflect admin-selected Moyasar mode in the registry row.
                    if (g.ProviderName.Equals(MoyasarPaymentGateway.Name, StringComparison.OrdinalIgnoreCase))
                        mode = moyasarMode;

                    return new PaymentGatewayInfoDto
                    {
                        Name = g.ProviderName,
                        IsConfigured = g.IsConfigured,
                        ClientMode = mode,
                        SupportsRefund = true
                    };
                })
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }
}
