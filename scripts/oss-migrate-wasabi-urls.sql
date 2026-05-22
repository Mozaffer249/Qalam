-- One-off: rewrite Wasabi URLs to Alibaba OSS (production bucket).
-- Run only after objects are copied to OSS with the same object keys.
-- Review counts before COMMIT; adjust bucket host if migrating to staging instead.

BEGIN TRANSACTION;

-- Inventory
SELECT COUNT(*) AS TeacherDocuments_Wasabi
FROM TeacherDocuments
WHERE FilePath LIKE '%wasabisys.com%';

SELECT COUNT(*) AS Users_Wasabi
FROM AspNetUsers
WHERE ProfilePictureUrl LIKE '%wasabisys.com%';

-- Production OSS virtual-hosted base (edit if needed)
DECLARE @OldProdPrefix NVARCHAR(200) = N'https://s3.ap-southeast-1.wasabisys.com/qalam-storage-prod/';
DECLARE @NewProdPrefix NVARCHAR(200) = N'https://auth-and-identities-certificates.oss-me-central-1.aliyuncs.com/';

DECLARE @OldStagingPrefix NVARCHAR(200) = N'https://s3.ap-southeast-1.wasabisys.com/qalam-storage-staging/';
DECLARE @NewStagingPrefix NVARCHAR(200) = N'https://auth-and-identities-certificates-staging.oss-me-central-1.aliyuncs.com/';

UPDATE TeacherDocuments
SET FilePath = REPLACE(FilePath, @OldProdPrefix, @NewProdPrefix)
WHERE FilePath LIKE @OldProdPrefix + N'%';

UPDATE TeacherDocuments
SET FilePath = REPLACE(FilePath, @OldStagingPrefix, @NewStagingPrefix)
WHERE FilePath LIKE @OldStagingPrefix + N'%';

UPDATE AspNetUsers
SET ProfilePictureUrl = REPLACE(ProfilePictureUrl, @OldProdPrefix, @NewProdPrefix)
WHERE ProfilePictureUrl LIKE @OldProdPrefix + N'%';

UPDATE AspNetUsers
SET ProfilePictureUrl = REPLACE(ProfilePictureUrl, @OldStagingPrefix, @NewStagingPrefix)
WHERE ProfilePictureUrl LIKE @OldStagingPrefix + N'%';

-- Legacy dev bucket name
UPDATE TeacherDocuments
SET FilePath = REPLACE(FilePath, N'https://s3.ap-southeast-1.wasabisys.com/qalam-storage/', @NewStagingPrefix)
WHERE FilePath LIKE N'https://s3.ap-southeast-1.wasabisys.com/qalam-storage/%';

UPDATE AspNetUsers
SET ProfilePictureUrl = REPLACE(ProfilePictureUrl, N'https://s3.ap-southeast-1.wasabisys.com/qalam-storage/', @NewStagingPrefix)
WHERE ProfilePictureUrl LIKE N'https://s3.ap-southeast-1.wasabisys.com/qalam-storage/%';

-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
