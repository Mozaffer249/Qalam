-- Full apply: teacher registration tables + default seed (emergency / manual)
-- Use when auto-migration did not run. Safe to re-run (idempotent).

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'teacher')
    EXEC(N'CREATE SCHEMA [teacher];');
GO

IF OBJECT_ID(N'teacher.TeacherRegistrationRequirements', N'U') IS NULL
BEGIN
    CREATE TABLE [teacher].[TeacherRegistrationRequirements] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(64) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [DescriptionAr] nvarchar(500) NULL,
        [DescriptionEn] nvarchar(500) NULL,
        [RequirementType] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsRequired] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [MinCount] int NOT NULL,
        [MaxCount] int NOT NULL,
        [MaxFileSizeBytes] int NOT NULL,
        [AllowedExtensionsJson] nvarchar(500) NOT NULL,
        [MaxLength] int NULL,
        [MapsToDocumentType] int NULL,
        [IsSystem] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        CONSTRAINT [PK_TeacherRegistrationRequirements] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_TeacherRegistrationRequirements_Code]
        ON [teacher].[TeacherRegistrationRequirements] ([Code]);

    CREATE INDEX [IX_TeacherRegistrationRequirements_IsActive_SortOrder]
        ON [teacher].[TeacherRegistrationRequirements] ([IsActive], [SortOrder]);
END
GO

IF OBJECT_ID(N'teacher.TeacherRegistrationSubmissions', N'U') IS NULL
BEGIN
    CREATE TABLE [teacher].[TeacherRegistrationSubmissions] (
        [Id] int NOT NULL IDENTITY,
        [TeacherId] int NOT NULL,
        [RequirementId] int NOT NULL,
        [VerificationStatus] int NOT NULL,
        [TextValue] nvarchar(2000) NULL,
        [BoolValue] bit NULL,
        [TeacherDocumentId] int NULL,
        [ReviewedByAdminId] int NULL,
        [ReviewedAt] datetime2 NULL,
        [RejectionReason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        CONSTRAINT [PK_TeacherRegistrationSubmissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TeacherRegistrationSubmissions_TeacherDocuments_TeacherDocumentId]
            FOREIGN KEY ([TeacherDocumentId]) REFERENCES [TeacherDocuments] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_TeacherRegistrationSubmissions_TeacherRegistrationRequirements_RequirementId]
            FOREIGN KEY ([RequirementId]) REFERENCES [teacher].[TeacherRegistrationRequirements] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TeacherRegistrationSubmissions_Teachers_TeacherId]
            FOREIGN KEY ([TeacherId]) REFERENCES [Teachers] ([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_TeacherRegistrationSubmissions_RequirementId]
        ON [teacher].[TeacherRegistrationSubmissions] ([RequirementId]);

    CREATE INDEX [IX_TeacherRegistrationSubmissions_TeacherDocumentId]
        ON [teacher].[TeacherRegistrationSubmissions] ([TeacherDocumentId]);

    CREATE UNIQUE INDEX [IX_TeacherRegistrationSubmissions_TeacherId_RequirementId]
        ON [teacher].[TeacherRegistrationSubmissions] ([TeacherId], [RequirementId]);

    CREATE INDEX [IX_TeacherRegistrationSubmissions_VerificationStatus]
        ON [teacher].[TeacherRegistrationSubmissions] ([VerificationStatus]);
END
GO

-- Seed default rows (see docs/seed-data/teacher-registration-requirements.json)
DECLARE @Now datetime2 = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM teacher.TeacherRegistrationRequirements WHERE Code = N'identity_document')
INSERT INTO teacher.TeacherRegistrationRequirements
    (Code, NameAr, NameEn, DescriptionAr, DescriptionEn, RequirementType, IsActive, IsRequired,
     SortOrder, MinCount, MaxCount, MaxFileSizeBytes, AllowedExtensionsJson, MaxLength,
     MapsToDocumentType, IsSystem, CreatedAt)
VALUES
    (N'identity_document', N'وثيقة الهوية', N'Identity document',
     N'هوية وطنية أو إقامة أو جواز سفر حسب موقعك',
     N'National ID, Iqama, or passport depending on location',
     1, 1, 1, 10, 1, 1, 10485760, N'[".pdf",".jpg",".jpeg",".png"]', NULL, 1, 1, @Now);

IF NOT EXISTS (SELECT 1 FROM teacher.TeacherRegistrationRequirements WHERE Code = N'certificate')
INSERT INTO teacher.TeacherRegistrationRequirements
    (Code, NameAr, NameEn, DescriptionAr, DescriptionEn, RequirementType, IsActive, IsRequired,
     SortOrder, MinCount, MaxCount, MaxFileSizeBytes, AllowedExtensionsJson, MaxLength,
     MapsToDocumentType, IsSystem, CreatedAt)
VALUES
    (N'certificate', N'الشهادات', N'Certificates',
     N'شهادة واحدة على الأقل (حتى 5)',
     N'At least one certificate (up to 5)',
     1, 1, 1, 20, 1, 5, 10485760, N'[".pdf",".jpg",".jpeg",".png"]', NULL, 2, 1, @Now);

IF NOT EXISTS (SELECT 1 FROM teacher.TeacherRegistrationRequirements WHERE Code = N'bio')
INSERT INTO teacher.TeacherRegistrationRequirements
    (Code, NameAr, NameEn, DescriptionAr, DescriptionEn, RequirementType, IsActive, IsRequired,
     SortOrder, MinCount, MaxCount, MaxFileSizeBytes, AllowedExtensionsJson, MaxLength,
     MapsToDocumentType, IsSystem, CreatedAt)
VALUES
    (N'bio', N'نبذة عنك', N'Bio',
     N'نبذة قصيرة تظهر للطلاب',
     N'Short profile shown to students',
     2, 1, 0, 30, 0, 1, 10485760, N'[".pdf",".jpg",".jpeg",".png"]', 500, NULL, 1, @Now);

IF NOT EXISTS (SELECT 1 FROM teacher.TeacherRegistrationRequirements WHERE Code = N'location')
INSERT INTO teacher.TeacherRegistrationRequirements
    (Code, NameAr, NameEn, DescriptionAr, DescriptionEn, RequirementType, IsActive, IsRequired,
     SortOrder, MinCount, MaxCount, MaxFileSizeBytes, AllowedExtensionsJson, MaxLength,
     MapsToDocumentType, IsSystem, CreatedAt)
VALUES
    (N'location', N'موقع التدريس', N'Teaching location',
     N'هل تدرّس داخل المملكة؟',
     N'Are you teaching inside Saudi Arabia?',
     3, 1, 1, 40, 1, 1, 10485760, N'[".pdf",".jpg",".jpeg",".png"]', NULL, NULL, 1, @Now);

-- Record migration if EF history table exists and row is missing
IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM [__EFMigrationsHistory]
       WHERE [MigrationId] = N'20260604190011_AddTeacherRegistrationRequirements')
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260604190011_AddTeacherRegistrationRequirements', N'8.0.0');

SELECT Code, NameEn, RequirementType, IsActive, IsRequired, SortOrder
FROM teacher.TeacherRegistrationRequirements
ORDER BY SortOrder;
