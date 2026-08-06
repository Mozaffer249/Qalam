using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class OsrNotificationSettingsProvider : IOsrNotificationSettingsProvider
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ILogger<OsrNotificationSettingsProvider> _logger;

    public OsrNotificationSettingsProvider(
        ISystemSettingRepository systemSettingRepository,
        ILogger<OsrNotificationSettingsProvider> logger)
    {
        _systemSettingRepository = systemSettingRepository;
        _logger = logger;
    }

    public async Task<OsrNotificationSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var row = await _systemSettingRepository.GetByKeyAsync(
            OsrNotificationSettingsKeys.SettingsKey,
            cancellationToken);
        if (row != null)
        {
            try
            {
                return OsrNotificationSettingsDefaults.FromJson(row.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid OSR.Notifications JSON; using defaults");
                return OsrNotificationSettingsDefaults.Create();
            }
        }

        return OsrNotificationSettingsDefaults.Create();
    }

    public async Task<OsrNotificationSettingsDto> SaveSettingsAsync(
        OsrNotificationSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        await _systemSettingRepository.UpsertAsync(new SystemSetting
        {
            Key = OsrNotificationSettingsKeys.SettingsKey,
            Value = OsrNotificationSettingsDefaults.ToJson(settings),
            Type = SettingType.JSON,
            IsPublic = false,
            DescriptionEn = "OSR match/target notification channels (email, SMS, push)",
            DescriptionAr = "قنوات إشعار مطابقة طلبات الجلسات (بريد، رسالة، دفع)"
        }, cancellationToken);

        return settings;
    }
}
