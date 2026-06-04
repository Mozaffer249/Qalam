-- Fix EF history when Scenario2 was applied under an old migration id (20260523151604)
-- but the codebase uses 20260523200422. Run once, then restart API or `dotnet ef database update`.

IF EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523151604_Scenario2_OpenSessionRequests_Initial')
   AND NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523200422_Scenario2_OpenSessionRequests_Initial')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523200422_Scenario2_OpenSessionRequests_Initial', N'8.0.16');
    PRINT 'Inserted 20260523200422_Scenario2 migration history row.';
END

SELECT [MigrationId] FROM [__EFMigrationsHistory]
WHERE [MigrationId] LIKE N'20260523%' OR [MigrationId] LIKE N'20260604%'
ORDER BY [MigrationId];
