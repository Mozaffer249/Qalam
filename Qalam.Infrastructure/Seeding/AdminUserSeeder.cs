using Microsoft.AspNetCore.Identity;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Identity;

namespace Qalam.Infrastructure.Seeding;

public static class AdminUserSeeder
{
    public static async Task SeedAsync(UserManager<User> userManager)
    {
        var adminEmail = "admin@qalam.com";
        
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
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

            var result = await userManager.CreateAsync(admin, "Admin@123");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
            }
        }
    }
}
