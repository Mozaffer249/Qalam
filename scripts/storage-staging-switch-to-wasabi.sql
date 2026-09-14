-- Staging: rewrite Alibaba OSS public URLs → Wasabi (same bucket names / object keys).
-- Target database: qalam_staging (NOT qalam_prod).
-- Tables use app schemas (security, course, sr, teacher), not dbo.AspNetUsers.

USE qalam_staging;
GO

IF DB_NAME() <> N'qalam_staging'
BEGIN
    THROW 50001, N'Refused: this script must run on qalam_staging only.', 1;
END
GO

SET NOCOUNT ON;

DECLARE @OldIdentities NVARCHAR(400);
DECLARE @NewIdentities NVARCHAR(400);
DECLARE @OldLearning NVARCHAR(400);
DECLARE @NewLearning NVARCHAR(400);

SET @OldIdentities = N'https://auth-and-identities-certificates-staging.oss-me-central-1.aliyuncs.com/';
SET @NewIdentities = N'https://auth-and-identities-certificates-staging.s3.ap-southeast-2.wasabisys.com/';
SET @OldLearning = N'https://qalam-content-stg.oss-me-central-1.aliyuncs.com/';
SET @NewLearning = N'https://qalam-content-stg.s3.ap-southeast-2.wasabisys.com/';

BEGIN TRANSACTION;

SELECT COUNT(*) AS TeacherDocuments_Oss
FROM dbo.TeacherDocuments
WHERE FilePath LIKE @OldIdentities + N'%';

SELECT COUNT(*) AS Users_Oss
FROM security.Users
WHERE ProfilePictureUrl LIKE @OldIdentities + N'%';

SELECT COUNT(*) AS Courses_Oss
FROM course.Courses
WHERE ImageUrl LIKE @OldLearning + N'%';

SELECT COUNT(*) AS OsrAttachments_Oss
FROM sr.SessionRequestAttachments
WHERE PublicUrl LIKE @OldLearning + N'%';

SELECT COUNT(*) AS TeacherContent_Oss
FROM teacher.TeacherContentItems
WHERE PublicUrl LIKE @OldLearning + N'%';

SELECT COUNT(*) AS ComplaintAttachments_Oss
FROM course.SessionComplaintAttachments
WHERE FileUrl LIKE @OldLearning + N'%';

UPDATE dbo.TeacherDocuments
SET FilePath = REPLACE(FilePath, @OldIdentities, @NewIdentities)
WHERE FilePath LIKE @OldIdentities + N'%';

-- Skip ProfilePictureUrl: staging profiles/ was deleted on OSS and not copied to Wasabi.
-- UPDATE security.Users
-- SET ProfilePictureUrl = REPLACE(ProfilePictureUrl, @OldIdentities, @NewIdentities)
-- WHERE ProfilePictureUrl LIKE @OldIdentities + N'%';

UPDATE course.Courses
SET ImageUrl = REPLACE(ImageUrl, @OldLearning, @NewLearning)
WHERE ImageUrl LIKE @OldLearning + N'%';

UPDATE sr.SessionRequestAttachments
SET PublicUrl = REPLACE(PublicUrl, @OldLearning, @NewLearning)
WHERE PublicUrl LIKE @OldLearning + N'%';

UPDATE teacher.TeacherContentItems
SET PublicUrl = REPLACE(PublicUrl, @OldLearning, @NewLearning)
WHERE PublicUrl LIKE @OldLearning + N'%';

UPDATE course.SessionComplaintAttachments
SET FileUrl = REPLACE(FileUrl, @OldLearning, @NewLearning)
WHERE FileUrl LIKE @OldLearning + N'%';

-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
