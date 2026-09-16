-- Production rollback: Wasabi Sydney → Alibaba OSS. Database: qalam_prod only.
-- Batched COMMITs so the API is not locked out.

USE qalam_prod;
GO

IF DB_NAME() <> N'qalam_prod'
BEGIN
    THROW 50001, N'Refused: this script must run on qalam_prod only.', 1;
END
GO

SET NOCOUNT ON;
SET DEADLOCK_PRIORITY LOW;
SET XACT_ABORT ON;

DECLARE @OldIdentities NVARCHAR(400);
DECLARE @NewIdentities NVARCHAR(400);
DECLARE @OldLearning NVARCHAR(400);
DECLARE @NewLearning NVARCHAR(400);
DECLARE @BatchSize INT;
DECLARE @Rows INT;

SET @OldIdentities = N'https://auth-and-identities-certificates.s3.ap-southeast-2.wasabisys.com/';
SET @NewIdentities = N'https://auth-and-identities-certificates.oss-me-central-1.aliyuncs.com/';
SET @OldLearning = N'https://qalam-content-prod.s3.ap-southeast-2.wasabisys.com/';
SET @NewLearning = N'https://qalam-content-prod.oss-me-central-1.aliyuncs.com/';
SET @BatchSize = 200;

SELECT COUNT(*) AS TeacherDocuments_Wasabi FROM dbo.TeacherDocuments WITH (NOLOCK) WHERE FilePath LIKE @OldIdentities + N'%';
SELECT COUNT(*) AS Users_Wasabi FROM security.Users WITH (NOLOCK) WHERE ProfilePictureUrl LIKE @OldIdentities + N'%';
SELECT COUNT(*) AS Courses_Wasabi FROM course.Courses WITH (NOLOCK) WHERE ImageUrl LIKE @OldLearning + N'%';
SELECT COUNT(*) AS OsrAttachments_Wasabi FROM sr.SessionRequestAttachments WITH (NOLOCK) WHERE PublicUrl LIKE @OldLearning + N'%';
SELECT COUNT(*) AS TeacherContent_Wasabi FROM teacher.TeacherContentItems WITH (NOLOCK) WHERE PublicUrl LIKE @OldLearning + N'%';
SELECT COUNT(*) AS ComplaintAttachments_Wasabi FROM course.SessionComplaintAttachments WITH (NOLOCK) WHERE FileUrl LIKE @OldLearning + N'%';

SET @Rows = 1;
WHILE @Rows > 0
BEGIN
    BEGIN TRANSACTION;
    UPDATE TOP (@BatchSize) dbo.TeacherDocuments WITH (ROWLOCK)
    SET FilePath = REPLACE(FilePath, @OldIdentities, @NewIdentities)
    WHERE FilePath LIKE @OldIdentities + N'%';
    SET @Rows = @@ROWCOUNT;
    COMMIT TRANSACTION;
END

SET @Rows = 1;
WHILE @Rows > 0
BEGIN
    BEGIN TRANSACTION;
    UPDATE TOP (@BatchSize) security.Users WITH (ROWLOCK)
    SET ProfilePictureUrl = REPLACE(ProfilePictureUrl, @OldIdentities, @NewIdentities)
    WHERE ProfilePictureUrl LIKE @OldIdentities + N'%';
    SET @Rows = @@ROWCOUNT;
    COMMIT TRANSACTION;
END

SET @Rows = 1;
WHILE @Rows > 0
BEGIN
    BEGIN TRANSACTION;
    UPDATE TOP (@BatchSize) course.Courses WITH (ROWLOCK)
    SET ImageUrl = REPLACE(ImageUrl, @OldLearning, @NewLearning)
    WHERE ImageUrl LIKE @OldLearning + N'%';
    SET @Rows = @@ROWCOUNT;
    COMMIT TRANSACTION;
END

SET @Rows = 1;
WHILE @Rows > 0
BEGIN
    BEGIN TRANSACTION;
    UPDATE TOP (@BatchSize) sr.SessionRequestAttachments WITH (ROWLOCK)
    SET PublicUrl = REPLACE(PublicUrl, @OldLearning, @NewLearning)
    WHERE PublicUrl LIKE @OldLearning + N'%';
    SET @Rows = @@ROWCOUNT;
    COMMIT TRANSACTION;
END

SET @Rows = 1;
WHILE @Rows > 0
BEGIN
    BEGIN TRANSACTION;
    UPDATE TOP (@BatchSize) teacher.TeacherContentItems WITH (ROWLOCK)
    SET PublicUrl = REPLACE(PublicUrl, @OldLearning, @NewLearning)
    WHERE PublicUrl LIKE @OldLearning + N'%';
    SET @Rows = @@ROWCOUNT;
    COMMIT TRANSACTION;
END

SET @Rows = 1;
WHILE @Rows > 0
BEGIN
    BEGIN TRANSACTION;
    UPDATE TOP (@BatchSize) course.SessionComplaintAttachments WITH (ROWLOCK)
    SET FileUrl = REPLACE(FileUrl, @OldLearning, @NewLearning)
    WHERE FileUrl LIKE @OldLearning + N'%';
    SET @Rows = @@ROWCOUNT;
    COMMIT TRANSACTION;
END
