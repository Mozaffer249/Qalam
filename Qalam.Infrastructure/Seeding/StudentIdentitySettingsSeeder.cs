using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class StudentIdentitySettingsSeeder
{
    private const string SettingKey = "StudentIdentityRules";

    private const string DefaultJson = """
        {
          "rules": [
            { "nationalityCode": "SA", "allowedIdentityTypes": [1], "issuingCountryRequired": false },
            { "nationalityCode": "*",  "allowedIdentityTypes": [2, 3], "issuingCountryRequired": true }
          ],
          "identityRequiredForEnrollment": false
        }
        """;

    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "common", "SystemSettings"))
            return;

        var exists = await context.SystemSettings
            .AnyAsync(s => s.Key == SettingKey);
        if (exists) return;

        await context.SystemSettings.AddAsync(new SystemSetting
        {
            Key = SettingKey,
            Value = DefaultJson,
            Type = SettingType.JSON,
            IsPublic = true,
            DescriptionEn = "Student identity validation rules per nationality (allowed document types, issuing country requirement)",
            DescriptionAr = "قواعد التحقق من هوية الطالب حسب الجنسية (أنواع الوثائق المسموحة، متطلبات بلد الإصدار)",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
