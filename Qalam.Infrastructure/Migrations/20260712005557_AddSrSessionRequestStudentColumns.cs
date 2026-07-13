using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSrSessionRequestStudentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('sr.SessionRequests', 'StudentId') IS NULL
                BEGIN
                    ALTER TABLE [sr].[SessionRequests] ADD [StudentId] int NULL;
                END

                IF COL_LENGTH('sr.SessionRequests', 'CreatedByGuardianId') IS NULL
                BEGIN
                    ALTER TABLE [sr].[SessionRequests] ADD [CreatedByGuardianId] int NULL;
                END

                UPDATE r
                SET r.StudentId = s.Id
                FROM [sr].[SessionRequests] r
                INNER JOIN [student].[Students] s ON s.UserId = r.RequestedByUserId
                WHERE r.StudentId IS NULL;

                IF COL_LENGTH('sr.SessionRequests', 'StudentId') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM [sr].[SessionRequests] WHERE [StudentId] IS NULL)
                   AND EXISTS (
                       SELECT 1 FROM sys.columns c
                       INNER JOIN sys.tables t ON c.object_id = t.object_id
                       INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                       WHERE s.name = N'sr' AND t.name = N'SessionRequests'
                         AND c.name = N'StudentId' AND c.is_nullable = 1)
                BEGIN
                    ALTER TABLE [sr].[SessionRequests] ALTER COLUMN [StudentId] int NOT NULL;
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_SessionRequests_StudentId'
                      AND object_id = OBJECT_ID(N'sr.SessionRequests'))
                BEGIN
                    CREATE INDEX [IX_SessionRequests_StudentId]
                        ON [sr].[SessionRequests] ([StudentId]);
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_SessionRequests_CreatedByGuardianId'
                      AND object_id = OBJECT_ID(N'sr.SessionRequests'))
                BEGIN
                    CREATE INDEX [IX_SessionRequests_CreatedByGuardianId]
                        ON [sr].[SessionRequests] ([CreatedByGuardianId]);
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_SessionRequests_Students_StudentId')
                BEGIN
                    ALTER TABLE [sr].[SessionRequests] WITH CHECK
                    ADD CONSTRAINT [FK_SessionRequests_Students_StudentId]
                        FOREIGN KEY ([StudentId]) REFERENCES [student].[Students] ([Id]);
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_SessionRequests_Guardians_CreatedByGuardianId')
                BEGIN
                    ALTER TABLE [sr].[SessionRequests] WITH CHECK
                    ADD CONSTRAINT [FK_SessionRequests_Guardians_CreatedByGuardianId]
                        FOREIGN KEY ([CreatedByGuardianId]) REFERENCES [student].[Guardians] ([Id]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SessionRequests_Guardians_CreatedByGuardianId')
                    ALTER TABLE [sr].[SessionRequests] DROP CONSTRAINT [FK_SessionRequests_Guardians_CreatedByGuardianId];

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SessionRequests_Students_StudentId')
                    ALTER TABLE [sr].[SessionRequests] DROP CONSTRAINT [FK_SessionRequests_Students_StudentId];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SessionRequests_CreatedByGuardianId' AND object_id = OBJECT_ID(N'sr.SessionRequests'))
                    DROP INDEX [IX_SessionRequests_CreatedByGuardianId] ON [sr].[SessionRequests];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SessionRequests_StudentId' AND object_id = OBJECT_ID(N'sr.SessionRequests'))
                    DROP INDEX [IX_SessionRequests_StudentId] ON [sr].[SessionRequests];

                IF COL_LENGTH('sr.SessionRequests', 'CreatedByGuardianId') IS NOT NULL
                    ALTER TABLE [sr].[SessionRequests] DROP COLUMN [CreatedByGuardianId];

                IF COL_LENGTH('sr.SessionRequests', 'StudentId') IS NOT NULL
                    ALTER TABLE [sr].[SessionRequests] DROP COLUMN [StudentId];
                """);
        }
    }
}
