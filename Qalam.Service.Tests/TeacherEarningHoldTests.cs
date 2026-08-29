using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.context;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class TeacherEarningHoldTests
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

    private static async Task<CourseSchedule> SeedCompletedScheduleAsync(ApplicationDBContext db)
    {
        var snapshot = new PricingSnapshot
        {
            PricePerHour = 100m,
            TotalMinutes = 60,
            TotalPrice = 100m,
            TeacherSharePct = 70m,
            TeacherEarnings = 70m,
            PlatformShare = 30m,
            Currency = "SAR",
            MarketCode = "SA",
            SessionTypeCode = "individual",
            CreatedAt = DateTime.UtcNow,
        };
        db.PricingSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = 5,
            ApprovedAt = DateTime.UtcNow,
            AmountDue = 100m,
            PricingSnapshotId = snapshot.Id,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var schedule = new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DurationMinutes = 60,
            Status = ScheduleStatus.Completed,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        db.CourseSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule;
    }

    [Fact]
    public async Task Accrue_WithOnHoldStatus_CreatesOnHoldLine()
    {
        await using var db = CreateDb();
        var schedule = await SeedCompletedScheduleAsync(db);
        var sut = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);

        await sut.AccrueForCompletedScheduleAsync(schedule.Id, TeacherEarningLineStatus.OnHold);

        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(TeacherEarningLineStatus.OnHold, line.Status);
    }

    [Fact]
    public async Task PayoutPending_ExcludesOnHoldLines()
    {
        await using var db = CreateDb();
        var schedule = await SeedCompletedScheduleAsync(db);
        var earning = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);
        await earning.AccrueForCompletedScheduleAsync(schedule.Id, TeacherEarningLineStatus.OnHold);

        var payout = new PayoutService(db);
        var pending = await payout.ListPendingEarningsAsync();

        Assert.Empty(pending);
    }

    [Fact]
    public async Task PayoutPending_IncludesPendingAfterRelease()
    {
        await using var db = CreateDb();
        var schedule = await SeedCompletedScheduleAsync(db);
        var earning = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);
        await earning.AccrueForCompletedScheduleAsync(schedule.Id, TeacherEarningLineStatus.OnHold);

        var line = db.TeacherEarningLines.Single();
        line.Status = TeacherEarningLineStatus.Pending;
        await db.SaveChangesAsync();

        var pendingLine = db.TeacherEarningLines.Single(l => l.Status == TeacherEarningLineStatus.Pending);
        Assert.Equal(TeacherEarningLineStatus.Pending, pendingLine.Status);
    }
}
