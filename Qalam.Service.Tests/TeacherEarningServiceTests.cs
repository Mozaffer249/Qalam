using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.context;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class TeacherEarningServiceTests
{
    private static ApplicationDBContext CreateDb()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDBContext(options, config);
    }

    private static async Task SeedStarterLevelAsync(ApplicationDBContext db, decimal sharePct = 70m)
    {
        db.Set<TeacherLevel>().Add(new TeacherLevel
        {
            Code = "starter",
            NameEn = "Starter",
            NameAr = "Starter",
            TeacherSharePct = sharePct,
            OrderIndex = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task<(Enrollment enrollment, CourseSchedule first, CourseSchedule second)> SeedTwoSessionEnrollmentAsync(
        ApplicationDBContext db,
        bool isFreeTrial,
        decimal teacherEarnings,
        int totalMinutes = 120,
        decimal teacherSharePct = 70m)
    {
        var snapshot = new PricingSnapshot
        {
            PricePerHour = 100m,
            TotalMinutes = totalMinutes,
            TotalPrice = isFreeTrial ? 100m : 200m,
            TeacherSharePct = teacherSharePct,
            TeacherEarnings = teacherEarnings,
            PlatformShare = isFreeTrial ? 30m : 60m,
            Currency = "SAR",
            MarketCode = "SA",
            SessionTypeCode = "individual",
            CreatedAt = DateTime.UtcNow
        };
        db.PricingSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = 5,
            ApprovedAt = DateTime.UtcNow,
            IsFreeTrial = isFreeTrial,
            AmountDue = snapshot.TotalPrice,
            PricingSnapshotId = snapshot.Id,
            PricingSnapshot = snapshot,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var first = new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DurationMinutes = 60,
            Status = ScheduleStatus.Completed,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow
        };
        var second = new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
            DurationMinutes = 60,
            Status = ScheduleStatus.Completed,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow
        };
        db.CourseSchedules.AddRange(first, second);
        await db.SaveChangesAsync();
        return (enrollment, first, second);
    }

    [Fact]
    public async Task Accrue_StudentFreeTrial_TeacherEarnsEverySession()
    {
        await using var db = CreateDb();
        // Student free trial only reduces AmountDue; teacher snapshot keeps the full 140.
        var (_, first, second) = await SeedTwoSessionEnrollmentAsync(db, isFreeTrial: true, teacherEarnings: 140m);
        var sut = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);

        await sut.AccrueForCompletedScheduleAsync(first.Id);
        await sut.AccrueForCompletedScheduleAsync(second.Id);

        var lines = db.TeacherEarningLines.OrderBy(l => l.CourseScheduleId).ToList();
        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal(70m, l.Amount));
        Assert.All(lines, l => Assert.Equal(TeacherEarningSource.SessionCompleted, l.Source));
    }

    [Fact]
    public async Task Accrue_TeacherInterviewSchedule_IsUnpaid_OtherSessionsPaid()
    {
        await using var db = CreateDb();
        var (_, first, second) = await SeedTwoSessionEnrollmentAsync(db, isFreeTrial: false, teacherEarnings: 140m);
        db.Teachers.Add(new Teacher
        {
            Id = 5,
            HasCompletedInterviewSession = true,
            InterviewUnlockSource = InterviewUnlockSource.AutoFromSession,
            InterviewUnlockCourseScheduleId = first.Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var sut = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);

        await sut.AccrueForCompletedScheduleAsync(first.Id);
        Assert.Empty(db.TeacherEarningLines.ToList());

        await sut.AccrueForCompletedScheduleAsync(second.Id);
        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(70m, line.Amount);
        Assert.Equal(second.Id, line.CourseScheduleId);
    }

    [Fact]
    public async Task Accrue_NonFreeTrial_FirstScheduleEarnsProratedShare()
    {
        await using var db = CreateDb();
        var (_, first, _) = await SeedTwoSessionEnrollmentAsync(db, isFreeTrial: false, teacherEarnings: 140m);
        var sut = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);

        await sut.AccrueForCompletedScheduleAsync(first.Id);
        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(70m, line.Amount);
        Assert.Equal(TeacherEarningSource.SessionCompleted, line.Source);
    }

    [Fact]
    public async Task Accrue_LegacyZeroShareSnapshot_AccruesNothing()
    {
        await using var db = CreateDb();
        var (_, first, _) = await SeedTwoSessionEnrollmentAsync(
            db, isFreeTrial: true, teacherEarnings: 0m, teacherSharePct: 0m);
        var sut = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);

        await sut.AccrueForCompletedScheduleAsync(first.Id);
        Assert.Empty(db.TeacherEarningLines.ToList());
    }

    [Fact]
    public async Task Recompute_DryRunReportsChanges_RealRunRepricesSnapshotAndBackfillsLines()
    {
        await using var db = CreateDb();
        await SeedStarterLevelAsync(db, sharePct: 70m);
        // Legacy snapshot frozen at 0% while interview was pending; both sessions completed, no lines.
        var (enrollment, first, second) = await SeedTwoSessionEnrollmentAsync(
            db, isFreeTrial: true, teacherEarnings: 0m, teacherSharePct: 0m);
        enrollment.PricingSnapshot!.TeacherId = 5;
        enrollment.PricingSnapshot.PlatformShare = 100m;
        db.Teachers.Add(new Teacher
        {
            Id = 5,
            HasCompletedInterviewSession = true,
            InterviewUnlockSource = InterviewUnlockSource.AutoFromSession,
            InterviewUnlockCourseScheduleId = first.Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var audit = new Moq.Mock<Qalam.Service.Abstracts.IAuditService>();
        var sut = new TeacherEarningRecomputeService(
            db,
            new Qalam.Infrastructure.Repositories.TeacherLevelRepository(db),
            audit.Object,
            NullLogger<TeacherEarningRecomputeService>.Instance);

        var dry = await sut.RecomputeAsync(dryRun: true, adminUserId: 1);
        Assert.Equal(1, dry.SnapshotsUpdated);
        Assert.Equal(1, dry.LinesCreated);
        Assert.Equal(0m, db.PricingSnapshots.Single().TeacherEarnings);
        Assert.Empty(db.TeacherEarningLines.ToList());

        var real = await sut.RecomputeAsync(dryRun: false, adminUserId: 1);
        Assert.Equal(1, real.SnapshotsUpdated);
        var snap = db.PricingSnapshots.Single();
        Assert.Equal(70m, snap.TeacherSharePct);
        Assert.Equal(140m, snap.TeacherEarnings);
        Assert.Equal(-40m, snap.PlatformShare);
        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(second.Id, line.CourseScheduleId);
        Assert.Equal(70m, line.Amount);
        audit.Verify(a => a.LogAsync(
            "RecomputeTeacherEarnings", 1, Moq.It.IsAny<string>(), true,
            Moq.It.IsAny<string?>(), Moq.It.IsAny<string?>(), Moq.It.IsAny<string?>(),
            "TeacherEarnings", Moq.It.IsAny<string?>()), Moq.Times.Once);
    }
}
