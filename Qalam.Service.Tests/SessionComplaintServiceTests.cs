using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
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

public class SessionComplaintServiceTests
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

    private static async Task<(CourseSchedule schedule, int studentId)> SeedCompletedSessionAsync(ApplicationDBContext db)
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

        db.EnrollmentParticipants.Add(new EnrollmentParticipant
        {
            EnrollmentId = enrollment.Id,
            StudentId = 42,
            PaymentStatus = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        });

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
        return (schedule, 42);
    }

    private static SessionComplaintService CreateSut(ApplicationDBContext db, Mock<IRefundService>? refundMock = null)
    {
        var auditRepo = new SessionAuditLogRepository(db);
        var audit = new SessionAuditService(auditRepo);
        var complaintRepo = new SessionComplaintRepository(db);
        var scheduleRepo = new CourseScheduleRepository(db);
        var earning = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);
        var refund = refundMock ?? new Mock<IRefundService>();
        var fileStorage = new Mock<IFileStorageService>();
        fileStorage
            .Setup(f => f.ValidateFileAsync(It.IsAny<IFormFile>(), It.IsAny<string[]>(), It.IsAny<long>()))
            .ReturnsAsync(true);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OssSettings:LearningPublicBaseUrl"] = "https://cdn.example.com",
            })
            .Build();
        return new SessionComplaintService(
            complaintRepo,
            scheduleRepo,
            audit,
            earning,
            refund.Object,
            fileStorage.Object,
            config);
    }

    [Fact]
    public async Task FileComplaint_CreatesOpenComplaintAndAuditRow()
    {
        await using var db = CreateDb();
        var (schedule, studentId) = await SeedCompletedSessionAsync(db);
        var sut = CreateSut(db);

        var complaint = await sut.FileComplaintAsync(
            schedule.Id,
            studentId,
            userId: 99,
            SessionComplaintReason.TeacherNoShow,
            "Teacher did not join",
            attachments: null);

        Assert.Equal(SessionComplaintStatus.Open, complaint.Status);
        Assert.Single(db.SessionComplaints.ToList());
        Assert.Contains(db.SessionAuditLogs.ToList(), l => l.ActionType == SessionAuditActionType.ComplaintFiled);
    }

    [Fact]
    public async Task FileComplaint_BlocksDuplicateOpenComplaint()
    {
        await using var db = CreateDb();
        var (schedule, studentId) = await SeedCompletedSessionAsync(db);
        var sut = CreateSut(db);

        await sut.FileComplaintAsync(
            schedule.Id, studentId, 99, SessionComplaintReason.QualityIssue, "First", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FileComplaintAsync(
                schedule.Id, studentId, 99, SessionComplaintReason.TechnicalIssue, "Second", null));
    }

    [Fact]
    public async Task AssignAsync_MovesComplaintToInReview()
    {
        await using var db = CreateDb();
        var (schedule, studentId) = await SeedCompletedSessionAsync(db);
        var sut = CreateSut(db);

        var filed = await sut.FileComplaintAsync(
            schedule.Id, studentId, 99, SessionComplaintReason.TeacherLate, "Late", null);

        await sut.AssignAsync(filed.Id, adminUserId: 1, assignedToUserId: 2);

        var updated = await db.SessionComplaints.FindAsync(filed.Id);
        Assert.Equal(SessionComplaintStatus.InReview, updated!.Status);
        Assert.Equal(2, updated.AssignedToUserId);
    }

    [Fact]
    public async Task RequestTeacherResponse_SetsAwaitingTeacher()
    {
        await using var db = CreateDb();
        var (schedule, studentId) = await SeedCompletedSessionAsync(db);
        var sut = CreateSut(db);

        var filed = await sut.FileComplaintAsync(
            schedule.Id, studentId, 99, SessionComplaintReason.QualityIssue, "Bad audio", null);

        await sut.RequestTeacherResponseAsync(filed.Id, adminUserId: 1);

        var updated = await db.SessionComplaints.FindAsync(filed.Id);
        Assert.Equal(SessionComplaintStatus.AwaitingTeacher, updated!.Status);
        Assert.True(updated.RequiresTeacherResponse);
    }

    [Fact]
    public async Task ResolveAsync_NoAction_ReleasesEarningToPending()
    {
        await using var db = CreateDb();
        var (schedule, studentId) = await SeedCompletedSessionAsync(db);
        var sut = CreateSut(db);

        var earning = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);
        await earning.AccrueForCompletedScheduleAsync(schedule.Id, TeacherEarningLineStatus.OnHold);

        var filed = await sut.FileComplaintAsync(
            schedule.Id, studentId, 99, SessionComplaintReason.Other, "Issue", null);

        await sut.ResolveAsync(
            filed.Id,
            adminUserId: 1,
            SessionComplaintResolution.NoAction,
            resolutionNotes: "Closed",
            refundAmount: null,
            paymentId: null);

        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(TeacherEarningLineStatus.Pending, line.Status);
    }
}
