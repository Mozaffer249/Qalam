using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Auth;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class AuthSettingsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "common", "SystemSettings"))
            return;

        var exists = await context.SystemSettings
            .AnyAsync(s => s.Key == AuthSettingsKeys.SettingsKey);
        if (exists) return;

        var defaults = AuthSettingsDefaults.Create();
        await context.SystemSettings.AddAsync(new SystemSetting
        {
            Key = AuthSettingsKeys.SettingsKey,
            Value = AuthSettingsDefaults.ToJson(defaults),
            Type = SettingType.JSON,
            IsPublic = true,
            DescriptionEn = "Teacher and student login/register behaviour (OTP channel, required fields)",
            DescriptionAr = "إعدادات تسجيل الدخول والتسجيل للمعلم والطالب",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
