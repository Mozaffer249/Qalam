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
                return PaymentGatewaySettingsDefaults.FromJson(row.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Payments.Gateway JSON; using env fallback");
            }
        }

        return PaymentGatewaySettingsDefaults.Create(_envSettings.Provider);
    }

    public async Task<PaymentGatewaySettingsDto> SaveSettingsAsync(
        PaymentGatewaySettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        await _systemSettingRepository.UpsertAsync(new SystemSetting
        {
            Key = PaymentGatewaySettingsKeys.SettingsKey,
            Value = PaymentGatewaySettingsDefaults.ToJson(settings),
            Type = SettingType.JSON,
            IsPublic = false,
            DescriptionEn = "Active payment gateway for new payment intents",
            DescriptionAr = "بوابة الدفع النشطة لعمليات الدفع الجديدة"
        }, cancellationToken);

        return settings;
    }

    public async Task<PaymentGatewayAdminDto> GetAdminViewAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return new PaymentGatewayAdminDto
        {
            ActiveProvider = settings.ActiveProvider,
            Providers = _gateways
                .DistinctBy(g => g.ProviderName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new PaymentGatewayInfoDto
                {
                    Name = g.ProviderName,
                    IsConfigured = g.IsConfigured,
                    ClientMode = g.ClientMode.ToString(),
                    SupportsRefund = true
                })
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }
}
