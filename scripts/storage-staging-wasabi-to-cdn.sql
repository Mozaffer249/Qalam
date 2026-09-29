-- Staging: rewrite Wasabi direct URLs → Bunny CDN pull zones (same keys).
-- Target: qalam_staging only.
-- Run the WHOLE file (F5 with nothing selected). Variables live in one batch;
-- running only part of it fails with "Must declare the scalar variable".
-- Batched COMMITs so API queries are not locked out.
-- ProfilePictureUrl is included: only rows already on Wasabi are touched, and
-- those objects exist on Wasabi by definition.

USE qalam_staging;
GO

IF DB_NAME() <> N'qalam_staging'
BEGIN
    THROW 50001, N'Refused: this script must run on qalam_staging only.', 1;
END
GO

SET NOCOUNT ON;
SET DEADLOCK_PRIORITY LOW;
SET XACT_ABORT ON;

DECLARE @OldIdentities NVARCHAR(400) = N'https://auth-and-identities-certificates-staging.s3.ap-southeast-2.wasabisys.com/';
DECLARE @NewIdentities NVARCHAR(400) = N'https://qalam-id-stg.b-cdn.net/';
DECLARE @OldLearning NVARCHAR(400) = N'https://qalam-content-stg.s3.ap-southeast-2.wasabisys.com/';
DECLARE @NewLearning NVARCHAR(400) = N'https://qalam-content-stg.b-cdn.net/';
DECLARE @BatchSize INT = 200;
DECLARE @Rows INT;

SELECT 'before' AS Phase,
    (SELECT COUNT(*) FROM dbo.TeacherDocuments WITH (NOLOCK) WHERE FilePath LIKE @OldIdentities + N'%') AS TeacherDocuments,
    (SELECT COUNT(*) FROM security.Users WITH (NOLOCK) WHERE ProfilePictureUrl LIKE @OldIdentities + N'%') AS Users,
    (SELECT COUNT(*) FROM course.Courses WITH (NOLOCK) WHERE ImageUrl LIKE @OldLearning + N'%') AS Courses,
    (SELECT COUNT(*) FROM sr.SessionRequestAttachments WITH (NOLOCK) WHERE PublicUrl LIKE @OldLearning + N'%') AS OsrAttachments,
    (SELECT COUNT(*) FROM teacher.TeacherContentItems WITH (NOLOCK) WHERE PublicUrl LIKE @OldLearning + N'%') AS TeacherContent,
    (SELECT COUNT(*) FROM course.SessionComplaintAttachments WITH (NOLOCK) WHERE FileUrl LIKE @OldLearning + N'%') AS ComplaintAttachments;

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

SELECT 'after (expect all 0)' AS Phase,
    (SELECT COUNT(*) FROM dbo.TeacherDocuments WITH (NOLOCK) WHERE FilePath LIKE @OldIdentities + N'%') AS TeacherDocuments,
    (SELECT COUNT(*) FROM security.Users WITH (NOLOCK) WHERE ProfilePictureUrl LIKE @OldIdentities + N'%') AS Users,
    (SELECT COUNT(*) FROM course.Courses WITH (NOLOCK) WHERE ImageUrl LIKE @OldLearning + N'%') AS Courses,
    (SELECT COUNT(*) FROM sr.SessionRequestAttachments WITH (NOLOCK) WHERE PublicUrl LIKE @OldLearning + N'%') AS OsrAttachments,
    (SELECT COUNT(*) FROM teacher.TeacherContentItems WITH (NOLOCK) WHERE PublicUrl LIKE @OldLearning + N'%') AS TeacherContent,
    (SELECT COUNT(*) FROM course.SessionComplaintAttachments WITH (NOLOCK) WHERE FileUrl LIKE @OldLearning + N'%') AS ComplaintAttachments;

SELECT 'on CDN now' AS Phase,
    (SELECT COUNT(*) FROM dbo.TeacherDocuments WITH (NOLOCK) WHERE FilePath LIKE @NewIdentities + N'%') AS TeacherDocuments,
    (SELECT COUNT(*) FROM security.Users WITH (NOLOCK) WHERE ProfilePictureUrl LIKE @NewIdentities + N'%') AS Users,
    (SELECT COUNT(*) FROM course.Courses WITH (NOLOCK) WHERE ImageUrl LIKE @NewLearning + N'%') AS Courses,
    (SELECT COUNT(*) FROM sr.SessionRequestAttachments WITH (NOLOCK) WHERE PublicUrl LIKE @NewLearning + N'%') AS OsrAttachments,
    (SELECT COUNT(*) FROM teacher.TeacherContentItems WITH (NOLOCK) WHERE PublicUrl LIKE @NewLearning + N'%') AS TeacherContent,
    (SELECT COUNT(*) FROM course.SessionComplaintAttachments WITH (NOLOCK) WHERE FileUrl LIKE @NewLearning + N'%') AS ComplaintAttachments;
