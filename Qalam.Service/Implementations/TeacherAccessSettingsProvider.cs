using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherAccessSettingsProvider : ITeacherAccessSettingsProvider
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ILogger<TeacherAccessSettingsProvider> _logger;

    public TeacherAccessSettingsProvider(
        ISystemSettingRepository systemSettingRepository,
        ILogger<TeacherAccessSettingsProvider> logger)
    {
        _systemSettingRepository = systemSettingRepository;
        _logger = logger;
    }

    public async Task<TeacherAccessSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var row = await _systemSettingRepository.GetByKeyAsync(
            TeacherAccessSettingsKeys.SettingsKey,
            cancellationToken);
        if (row != null)
        {
            try
            {
                return TeacherAccessSettingsDefaults.FromJson(row.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Platform.TeacherAccess JSON; using defaults");
                return TeacherAccessSettingsDefaults.Create();
            }
        }

        return TeacherAccessSettingsDefaults.Create();
    }

    public async Task<TeacherAccessSettingsDto> SaveSettingsAsync(
        TeacherAccessSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        await _systemSettingRepository.UpsertAsync(new SystemSetting
        {
            Key = TeacherAccessSettingsKeys.SettingsKey,
            Value = TeacherAccessSettingsDefaults.ToJson(settings),
            Type = SettingType.JSON,
            IsPublic = false,
            DescriptionEn = "Whether activated teachers may enter the dashboard (platform launch gate)",
            DescriptionAr = "السماح للمعلمين المفعّلين بدخول لوحة التحكم (بوابة إطلاق المنصة)"
        }, cancellationToken);

        return settings;
    }
}
