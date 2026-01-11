using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeding;
using System;

namespace Qalam.Infrastructure.Migrations
{
    public static class MigrationExtensions
    {
        public static void SeedDatabase(this MigrationBuilder migrationBuilder, string connectionString)
        {
            try
            {
                // Create DbContext options
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
                optionsBuilder.UseSqlServer(connectionString);
                
                // Create temporary context and seed synchronously
                using (var context = new ApplicationDBContext(optionsBuilder.Options))
                {
                    // Call the async seeder synchronously
                    DatabaseSeeder.SeedAllAsync(context).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the migration
                migrationBuilder.Sql($"-- Seeding error: {ex.Message}");
                throw; // Re-throw to fail the migration if seeding fails
            }
        }
    }
}
