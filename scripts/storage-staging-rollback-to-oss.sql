-- Staging rollback: rewrite Wasabi public URLs → Alibaba OSS (same bucket names / object keys).
-- Use after STORAGE_PROVIDER=alibaba redeploy if you need DB URLs to match Alibaba again.
-- Review inventory counts before COMMIT.

BEGIN TRANSACTION;

DECLARE @OldIdentities NVARCHAR(300) =
    N'https://auth-and-identities-certificates-staging.s3.ap-southeast-1.wasabisys.com/';
DECLARE @NewIdentities NVARCHAR(300) =
    N'https://auth-and-identities-certificates-staging.oss-me-central-1.aliyuncs.com/';

DECLARE @OldLearning NVARCHAR(300) =
    N'https://qalam-content-stg.s3.ap-southeast-1.wasabisys.com/';
DECLARE @NewLearning NVARCHAR(300) =
    N'https://qalam-content-stg.oss-me-central-1.aliyuncs.com/';

-- Inventory
SELECT COUNT(*) AS TeacherDocuments_Wasabi
FROM TeacherDocuments
WHERE FilePath LIKE @OldIdentities + N'%';

SELECT COUNT(*) AS Users_Wasabi
FROM AspNetUsers
WHERE ProfilePictureUrl LIKE @OldIdentities + N'%';

SELECT COUNT(*) AS Courses_Wasabi
FROM Courses
WHERE ImageUrl LIKE @OldLearning + N'%';

SELECT COUNT(*) AS OsrAttachments_Wasabi
FROM OpenSessionRequestAttachments
WHERE PublicUrl LIKE @OldLearning + N'%';

SELECT COUNT(*) AS TeacherContent_Wasabi
FROM TeacherContentItems
WHERE PublicUrl LIKE @OldLearning + N'%';

SELECT COUNT(*) AS ComplaintAttachments_Wasabi
FROM SessionComplaintAttachments
WHERE FileUrl LIKE @OldLearning + N'%';

-- Identities
UPDATE TeacherDocuments
SET FilePath = REPLACE(FilePath, @OldIdentities, @NewIdentities)
WHERE FilePath LIKE @OldIdentities + N'%';

UPDATE AspNetUsers
SET ProfilePictureUrl = REPLACE(ProfilePictureUrl, @OldIdentities, @NewIdentities)
WHERE ProfilePictureUrl LIKE @OldIdentities + N'%';

-- Learning
UPDATE Courses
SET ImageUrl = REPLACE(ImageUrl, @OldLearning, @NewLearning)
WHERE ImageUrl LIKE @OldLearning + N'%';

UPDATE OpenSessionRequestAttachments
SET PublicUrl = REPLACE(PublicUrl, @OldLearning, @NewLearning)
WHERE PublicUrl LIKE @OldLearning + N'%';

UPDATE TeacherContentItems
SET PublicUrl = REPLACE(PublicUrl, @OldLearning, @NewLearning)
WHERE PublicUrl LIKE @OldLearning + N'%';

UPDATE SessionComplaintAttachments
SET FileUrl = REPLACE(FileUrl, @OldLearning, @NewLearning)
WHERE FileUrl LIKE @OldLearning + N'%';

-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
