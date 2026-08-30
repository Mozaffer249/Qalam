using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;

namespace Qalam.Service.Tests;

public class AdminSessionActionTests
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

    private static async Task<CourseSchedule> SeedScheduledSessionAsync(ApplicationDBContext db)
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
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
            DurationMinutes = 60,
            Status = ScheduleStatus.Scheduled,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        db.CourseSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule;
    }

    private static AdminSessionActionService CreateSut(ApplicationDBContext db)
    {
        var scheduleRepo = new CourseScheduleRepository(db);
        var auditRepo = new SessionAuditLogRepository(db);
        var audit = new SessionAuditService(auditRepo);
        var earning = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);
        var refundMock = new Mock<IRefundService>();
        var fileStorageMock = new Mock<IFileStorageService>();
        var ossConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OssSettings:LearningPublicBaseUrl"] = "https://cdn.example.com",
            })
            .Build();
        var complaints = ComplaintResolutionTestHelper.CreateComplaintService(db, refundMock);
        var teacherMgmtMock = new Mock<ITeacherManagementService>();
        var financeImpact = new TeacherFinanceImpactService(new TeacherFinanceImpactRepository(db));
        return new AdminSessionActionService(
            scheduleRepo, audit, complaints, refundMock.Object, teacherMgmtMock.Object, financeImpact);
    }

    [Fact]
    public async Task CancelAsync_WritesAuditRow()
    {
        await using var db = CreateDb();
        var schedule = await SeedScheduledSessionAsync(db);
        var sut = CreateSut(db);

        await sut.CancelAsync(schedule.Id, adminUserId: 11);

        var updated = await db.CourseSchedules.FindAsync(schedule.Id);
        Assert.Equal(ScheduleStatus.Cancelled, updated!.Status);
        Assert.Contains(
            db.SessionAuditLogs.ToList(),
            l => l.ActionType == SessionAuditActionType.SessionCancelled && l.ActorUserId == 11);
    }

    [Fact]
    public async Task VoidEarningAsync_WritesAuditAndVoidsLine()
    {
        await using var db = CreateDb();
        var schedule = await SeedScheduledSessionAsync(db);
        schedule.Status = ScheduleStatus.Completed;
        await db.SaveChangesAsync();

        var earning = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);
        await earning.AccrueForCompletedScheduleAsync(schedule.Id, TeacherEarningLineStatus.Pending);

        var sut = CreateSut(db);
        await sut.VoidEarningAsync(schedule.Id, adminUserId: 22);

        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(TeacherEarningLineStatus.Voided, line.Status);
        Assert.Contains(
            db.SessionAuditLogs.ToList(),
            l => l.ActionType == SessionAuditActionType.EarningVoided && l.ActorUserId == 22);
    }
}
