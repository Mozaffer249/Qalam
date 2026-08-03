using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class TeacherAccessSettingsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "common", "SystemSettings"))
            return;

        var exists = await context.SystemSettings
            .AnyAsync(s => s.Key == TeacherAccessSettingsKeys.SettingsKey);
        if (exists) return;

        var defaults = TeacherAccessSettingsDefaults.Create();
        await context.SystemSettings.AddAsync(new SystemSetting
        {
            Key = TeacherAccessSettingsKeys.SettingsKey,
            Value = TeacherAccessSettingsDefaults.ToJson(defaults),
            Type = SettingType.JSON,
            IsPublic = false,
            DescriptionEn = "Whether activated teachers may enter the dashboard (platform launch gate)",
            DescriptionAr = "السماح للمعلمين المفعّلين بدخول لوحة التحكم (بوابة إطلاق المنصة)",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
