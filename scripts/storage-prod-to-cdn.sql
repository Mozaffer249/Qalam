-- Production: rewrite object URLs → Bunny CDN pull zones (same keys).
-- Target: qalam_prod only.
-- Handles BOTH legacy hosts (Alibaba OSS and Wasabi direct), since prod rows may
-- hold either after the interrupted Alibaba → Wasabi switch.
-- Run the WHOLE file (F5 with nothing selected). Variables live in one batch.
-- Batched COMMITs so API queries are not locked out.

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

DECLARE @NewIdentities NVARCHAR(400) = N'https://qalam-id.b-cdn.net/';
DECLARE @NewLearning NVARCHAR(400) = N'https://qalam-content-prod.b-cdn.net/';
DECLARE @BatchSize INT = 200;
DECLARE @Rows INT;

DECLARE @Sources TABLE (Id INT PRIMARY KEY, Label NVARCHAR(40), OldIdentities NVARCHAR(400), OldLearning NVARCHAR(400));
INSERT INTO @Sources VALUES
    (1, N'alibaba', N'https://auth-and-identities-certificates.oss-me-central-1.aliyuncs.com/', N'https://qalam-content-prod.oss-me-central-1.aliyuncs.com/'),
    (2, N'wasabi',  N'https://auth-and-identities-certificates.s3.ap-southeast-2.wasabisys.com/', N'https://qalam-content-prod.s3.ap-southeast-2.wasabisys.com/');

SELECT 'before' AS Phase, s.Label AS Source,
    (SELECT COUNT(*) FROM dbo.TeacherDocuments WITH (NOLOCK) WHERE FilePath LIKE s.OldIdentities + N'%') AS TeacherDocuments,
    (SELECT COUNT(*) FROM security.Users WITH (NOLOCK) WHERE ProfilePictureUrl LIKE s.OldIdentities + N'%') AS Users,
    (SELECT COUNT(*) FROM course.Courses WITH (NOLOCK) WHERE ImageUrl LIKE s.OldLearning + N'%') AS Courses,
    (SELECT COUNT(*) FROM sr.SessionRequestAttachments WITH (NOLOCK) WHERE PublicUrl LIKE s.OldLearning + N'%') AS OsrAttachments,
    (SELECT COUNT(*) FROM teacher.TeacherContentItems WITH (NOLOCK) WHERE PublicUrl LIKE s.OldLearning + N'%') AS TeacherContent,
    (SELECT COUNT(*) FROM course.SessionComplaintAttachments WITH (NOLOCK) WHERE FileUrl LIKE s.OldLearning + N'%') AS ComplaintAttachments
FROM @Sources s
ORDER BY s.Id;

DECLARE @Id INT = 1;
DECLARE @OldIdentities NVARCHAR(400);
DECLARE @OldLearning NVARCHAR(400);

WHILE @Id <= 2
BEGIN
    SELECT @OldIdentities = OldIdentities, @OldLearning = OldLearning FROM @Sources WHERE Id = @Id;

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

    SET @Id += 1;
END

SELECT 'after (expect all 0)' AS Phase, s.Label AS Source,
    (SELECT COUNT(*) FROM dbo.TeacherDocuments WITH (NOLOCK) WHERE FilePath LIKE s.OldIdentities + N'%') AS TeacherDocuments,
    (SELECT COUNT(*) FROM security.Users WITH (NOLOCK) WHERE ProfilePictureUrl LIKE s.OldIdentities + N'%') AS Users,
    (SELECT COUNT(*) FROM course.Courses WITH (NOLOCK) WHERE ImageUrl LIKE s.OldLearning + N'%') AS Courses,
    (SELECT COUNT(*) FROM sr.SessionRequestAttachments WITH (NOLOCK) WHERE PublicUrl LIKE s.OldLearning + N'%') AS OsrAttachments,
    (SELECT COUNT(*) FROM teacher.TeacherContentItems WITH (NOLOCK) WHERE PublicUrl LIKE s.OldLearning + N'%') AS TeacherContent,
    (SELECT COUNT(*) FROM course.SessionComplaintAttachments WITH (NOLOCK) WHERE FileUrl LIKE s.OldLearning + N'%') AS ComplaintAttachments
FROM @Sources s
ORDER BY s.Id;

SELECT 'on CDN now' AS Phase,
    (SELECT COUNT(*) FROM dbo.TeacherDocuments WITH (NOLOCK) WHERE FilePath LIKE @NewIdentities + N'%') AS TeacherDocuments,
    (SELECT COUNT(*) FROM security.Users WITH (NOLOCK) WHERE ProfilePictureUrl LIKE @NewIdentities + N'%') AS Users,
    (SELECT COUNT(*) FROM course.Courses WITH (NOLOCK) WHERE ImageUrl LIKE @NewLearning + N'%') AS Courses,
    (SELECT COUNT(*) FROM sr.SessionRequestAttachments WITH (NOLOCK) WHERE PublicUrl LIKE @NewLearning + N'%') AS OsrAttachments,
    (SELECT COUNT(*) FROM teacher.TeacherContentItems WITH (NOLOCK) WHERE PublicUrl LIKE @NewLearning + N'%') AS TeacherContent,
    (SELECT COUNT(*) FROM course.SessionComplaintAttachments WITH (NOLOCK) WHERE FileUrl LIKE @NewLearning + N'%') AS ComplaintAttachments;
