using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Identity;

namespace Qalam.Infrastructure.Seeding;

public static class AdminUserSeeder
{
    /// <summary>
    /// Seeds the default SuperAdmin user only when explicitly enabled.
    /// Gated by env/config to prevent prod from ever boot-seeding a known credential.
    /// Reads (in order of precedence: env var → config key):
    ///   SEED_DEFAULT_ADMIN  (true/false; default true in Development, false elsewhere)
    ///   DEFAULT_ADMIN_EMAIL (required when SEED_DEFAULT_ADMIN=true)
    ///   DEFAULT_ADMIN_PASSWORD (required when SEED_DEFAULT_ADMIN=true)
    /// In Development the seeder defaults on with admin@qalam.local / Admin@123 if no values are supplied,
    /// so local dev stays frictionless.
    /// </summary>
    public static async Task SeedAsync(
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger? logger = null)
    {
        var aspEnv = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var isDevelopment = string.Equals(aspEnv, "Development", StringComparison.OrdinalIgnoreCase);

        var enabledRaw = configuration["SEED_DEFAULT_ADMIN"];
        var enabled = string.Equals(enabledRaw, "true", StringComparison.OrdinalIgnoreCase)
                   || (string.IsNullOrEmpty(enabledRaw) && isDevelopment);

        if (!enabled)
        {
            logger?.LogInformation("AdminUserSeeder: SEED_DEFAULT_ADMIN is not enabled — skipping default admin seed.");
            return;
        }

        var adminEmail = configuration["DEFAULT_ADMIN_EMAIL"]
                         ?? (isDevelopment ? "admin@qalam.local" : null);
        var adminPassword = configuration["DEFAULT_ADMIN_PASSWORD"]
                            ?? (isDevelopment ? "Admin@123" : null);

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger?.LogWarning(
                "AdminUserSeeder: SEED_DEFAULT_ADMIN=true but DEFAULT_ADMIN_EMAIL or DEFAULT_ADMIN_PASSWORD is missing — skipping seed.");
            return;
        }

        if (await userManager.FindByEmailAsync(adminEmail) != null)
        {
            logger?.LogInformation("AdminUserSeeder: admin user '{Email}' already exists — skipping.", adminEmail);
            return;
        }

        var admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Admin",
            EmailConfirmed = true,
            IsActive = true,
            PasswordChangedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
            logger?.LogInformation("AdminUserSeeder: created SuperAdmin user '{Email}'.", adminEmail);
        }
        else
        {
            logger?.LogError(
                "AdminUserSeeder: failed to create admin '{Email}': {Errors}",
                adminEmail,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
