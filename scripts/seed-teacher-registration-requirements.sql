-- Idempotent seed for teacher.TeacherRegistrationRequirements
-- Run after migration AddTeacherRegistrationRequirements.
-- RequirementType: File=1, Text=2, Boolean=3
-- MapsToDocumentType: IdentityDocument=1, Certificate=2, Other=3

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

SELECT Code, NameEn, RequirementType, IsActive, IsRequired, SortOrder, MinCount, MaxCount
FROM teacher.TeacherRegistrationRequirements
ORDER BY SortOrder;
