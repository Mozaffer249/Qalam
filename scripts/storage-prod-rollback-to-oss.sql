-- Production rollback: Wasabi Sydney → Alibaba OSS. Database: qalam_prod only.

USE qalam_prod;
GO

IF DB_NAME() <> N'qalam_prod'
BEGIN
    THROW 50001, N'Refused: this script must run on qalam_prod only.', 1;
END
GO

SET NOCOUNT ON;

DECLARE @OldIdentities NVARCHAR(400);
DECLARE @NewIdentities NVARCHAR(400);
DECLARE @OldLearning NVARCHAR(400);
DECLARE @NewLearning NVARCHAR(400);

SET @OldIdentities = N'https://auth-and-identities-certificates.s3.ap-southeast-2.wasabisys.com/';
SET @NewIdentities = N'https://auth-and-identities-certificates.oss-me-central-1.aliyuncs.com/';
SET @OldLearning = N'https://qalam-content-prod.s3.ap-southeast-2.wasabisys.com/';
SET @NewLearning = N'https://qalam-content-prod.oss-me-central-1.aliyuncs.com/';

BEGIN TRANSACTION;

SELECT COUNT(*) AS TeacherDocuments_Wasabi FROM dbo.TeacherDocuments WHERE FilePath LIKE @OldIdentities + N'%';
SELECT COUNT(*) AS Users_Wasabi FROM security.Users WHERE ProfilePictureUrl LIKE @OldIdentities + N'%';
SELECT COUNT(*) AS Courses_Wasabi FROM course.Courses WHERE ImageUrl LIKE @OldLearning + N'%';
SELECT COUNT(*) AS OsrAttachments_Wasabi FROM sr.SessionRequestAttachments WHERE PublicUrl LIKE @OldLearning + N'%';
SELECT COUNT(*) AS TeacherContent_Wasabi FROM teacher.TeacherContentItems WHERE PublicUrl LIKE @OldLearning + N'%';
SELECT COUNT(*) AS ComplaintAttachments_Wasabi FROM course.SessionComplaintAttachments WHERE FileUrl LIKE @OldLearning + N'%';

UPDATE dbo.TeacherDocuments SET FilePath = REPLACE(FilePath, @OldIdentities, @NewIdentities) WHERE FilePath LIKE @OldIdentities + N'%';
UPDATE security.Users SET ProfilePictureUrl = REPLACE(ProfilePictureUrl, @OldIdentities, @NewIdentities) WHERE ProfilePictureUrl LIKE @OldIdentities + N'%';
UPDATE course.Courses SET ImageUrl = REPLACE(ImageUrl, @OldLearning, @NewLearning) WHERE ImageUrl LIKE @OldLearning + N'%';
UPDATE sr.SessionRequestAttachments SET PublicUrl = REPLACE(PublicUrl, @OldLearning, @NewLearning) WHERE PublicUrl LIKE @OldLearning + N'%';
UPDATE teacher.TeacherContentItems SET PublicUrl = REPLACE(PublicUrl, @OldLearning, @NewLearning) WHERE PublicUrl LIKE @OldLearning + N'%';
UPDATE course.SessionComplaintAttachments SET FileUrl = REPLACE(FileUrl, @OldLearning, @NewLearning) WHERE FileUrl LIKE @OldLearning + N'%';

-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
