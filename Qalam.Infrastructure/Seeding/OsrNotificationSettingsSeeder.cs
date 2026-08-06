using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class OsrNotificationSettingsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "common", "SystemSettings"))
            return;

        var exists = await context.SystemSettings
            .AnyAsync(s => s.Key == OsrNotificationSettingsKeys.SettingsKey);
        if (exists) return;

        var defaults = OsrNotificationSettingsDefaults.Create();
        await context.SystemSettings.AddAsync(new SystemSetting
        {
            Key = OsrNotificationSettingsKeys.SettingsKey,
            Value = OsrNotificationSettingsDefaults.ToJson(defaults),
            Type = SettingType.JSON,
            IsPublic = false,
            DescriptionEn = "OSR match/target notification channels (email, SMS, push)",
            DescriptionAr = "قنوات إشعار مطابقة طلبات الجلسات (بريد، رسالة، دفع)",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
