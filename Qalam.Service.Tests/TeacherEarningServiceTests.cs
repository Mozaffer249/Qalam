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
    public async Task Accrue_FreeTrial_SkipsFirstSchedule_AccruesSecondOnEarnableMinutes()
    {
        await using var db = CreateDb();
        // Snapshot already reduced: full notional 140 → 70 after free first session.
        var (_, first, second) = await SeedTwoSessionEnrollmentAsync(db, isFreeTrial: true, teacherEarnings: 70m);
        var sut = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);

        await sut.AccrueForCompletedScheduleAsync(first.Id);
        Assert.Empty(db.TeacherEarningLines.ToList());

        await sut.AccrueForCompletedScheduleAsync(second.Id);
        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(70m, line.Amount);
        Assert.Equal(TeacherEarningSource.SessionCompleted, line.Source);
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
    public async Task Accrue_InterviewPendingFreeTrial_UsesProjectionOnSecondSchedule()
    {
        await using var db = CreateDb();
        await SeedStarterLevelAsync(db, sharePct: 70m);

        var snapshot = new PricingSnapshot
        {
            PricePerHour = 85m,
            TotalMinutes = 120,
            TotalPrice = 85m,
            TeacherSharePct = 0m,
            TeacherEarnings = 0m,
            PlatformShare = 85m,
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
            IsFreeTrial = true,
            AmountDue = 85m,
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

        var originalTotalPrice = snapshot.TotalPrice;
        var sut = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);

        await sut.AccrueForCompletedScheduleAsync(first.Id);
        Assert.Empty(db.TeacherEarningLines.ToList());

        await sut.AccrueForCompletedScheduleAsync(second.Id);
        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(59.5m, line.Amount);
        Assert.Equal(originalTotalPrice, snapshot.TotalPrice);
    }
}
