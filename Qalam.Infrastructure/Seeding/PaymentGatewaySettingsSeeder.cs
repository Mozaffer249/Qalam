using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class PaymentGatewaySettingsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "common", "SystemSettings"))
            return;

        var exists = await context.SystemSettings
            .AnyAsync(s => s.Key == PaymentGatewaySettingsKeys.SettingsKey);
        if (exists) return;

        // Default Mock; runtime env fallback in PaymentGatewaySettingsProvider when row is absent.
        var defaults = PaymentGatewaySettingsDefaults.Create("Mock");
        await context.SystemSettings.AddAsync(new SystemSetting
        {
            Key = PaymentGatewaySettingsKeys.SettingsKey,
            Value = PaymentGatewaySettingsDefaults.ToJson(defaults),
            Type = SettingType.JSON,
            IsPublic = false,
            DescriptionEn = "Active payment gateway and Moyasar client mode for new payment intents",
            DescriptionAr = "بوابة الدفع النشطة ووضع عرض ميسر لعمليات الدفع الجديدة",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
