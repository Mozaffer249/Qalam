using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Identity;
using System;
using System.Threading.Tasks;

namespace Qalam.Infrastructure.Seeder
{
    public class RoleSeeder
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(RoleManager<Role> roleManager, ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Checking and seeding roles...");

                // Seed SuperAdmin role
                await CreateRoleIfNotExistsAsync(Roles.SuperAdmin);

                // Seed Admin role
                await CreateRoleIfNotExistsAsync(Roles.Admin);

                // Seed Staff role
                await CreateRoleIfNotExistsAsync(Roles.Staff);

                // Seed Teacher role
                await CreateRoleIfNotExistsAsync(Roles.Teacher);

                // Seed Student role
                await CreateRoleIfNotExistsAsync(Roles.Student);

                // Seed Guardian role
                await CreateRoleIfNotExistsAsync(Roles.Guardian);

                _logger.LogInformation("Role seeding completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding roles.");
                throw;
            }
        }

        private async Task CreateRoleIfNotExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new Role()
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });

                if (result.Succeeded)
                {
                    _logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                }
                else
                {
                    _logger.LogWarning("Failed to create role '{RoleName}': {Errors}",
                        roleName, string.Join(", ", result.Errors));
                }
            }
            else
            {
                _logger.LogDebug("Role '{RoleName}' already exists.", roleName);
            }
        }
    }
}

