using Microsoft.AspNetCore.Identity;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Identity;

namespace Qalam.Infrastructure.Seeding;

public static class RolesSeeder
{
    public static async Task SeedAsync(RoleManager<Role> roleManager)
    {
        string[] roles = 
        { 
            Roles.SuperAdmin, 
            Roles.Admin, 
            Roles.Staff, 
            Roles.Teacher, 
            Roles.Student, 
            Roles.Parent, 
            Roles.Customer, 
            Roles.User 
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new Role { Name = role });
            }
        }
    }
}
