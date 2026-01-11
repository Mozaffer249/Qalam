using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration marks the point where initial data seeding should occur
            // The actual seeding is performed by DatabaseSeeder.SeedAllAsync()
            // which should be called after migrations are applied
            
            // We add a marker table to track that seeding migration has been applied
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '_SeedingHistory')
                BEGIN
                    CREATE TABLE [_SeedingHistory] (
                        [MigrationId] nvarchar(150) NOT NULL PRIMARY KEY,
                        [AppliedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        [SeedingCompleted] bit NOT NULL DEFAULT 0,
                        [SeedingCompletedAt] datetime2 NULL
                    );
                END
                
                IF NOT EXISTS (SELECT * FROM [_SeedingHistory] WHERE [MigrationId] = '20260111200000_SeedInitialData')
                BEGIN
                    INSERT INTO [_SeedingHistory] ([MigrationId], [AppliedAt], [SeedingCompleted])
                    VALUES ('20260111200000_SeedInitialData', GETUTCDATE(), 0);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeding history marker
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '_SeedingHistory')
                BEGIN
                    DELETE FROM [_SeedingHistory] WHERE [MigrationId] = '20260111200000_SeedInitialData';
                END
            ");
        }
    }
}
