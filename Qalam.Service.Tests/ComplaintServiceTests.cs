using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.context;

namespace Qalam.Service.Tests;

public class ComplaintServiceTests
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

    [Fact]
    public async Task FileOther_CreatesSubmittedComplaint()
    {
        await using var db = CreateDb();
        var sut = ComplaintResolutionTestHelper.CreateUnifiedComplaintService(db);

        var detail = await sut.FileAsync(
            complainantUserId: 99,
            ComplaintComplainantRole.Student,
            new FileComplaintRequest
            {
                SubjectType = ComplaintSubjectType.Other,
                ReasonCode = ComplaintReason.Other,
                Description = "General issue",
            },
            attachments: null);

        Assert.Equal("Submitted", detail.Status);
        Assert.Equal("Other", detail.SubjectType);
        Assert.Single(db.Complaints.ToList());
        Assert.Equal(99, detail.ComplainantUserId);
    }

    [Fact]
    public async Task RequestInfo_Complainant_SetsAwaitingComplainant()
    {
        await using var db = CreateDb();
        var sut = ComplaintResolutionTestHelper.CreateUnifiedComplaintService(db);

        var filed = await sut.FileAsync(
            99,
            ComplaintComplainantRole.Student,
            new FileComplaintRequest
            {
                SubjectType = ComplaintSubjectType.Other,
                ReasonCode = ComplaintReason.Other,
                Description = "Need follow-up",
            },
            null);

        await sut.RequestInfoAsync(filed.ComplaintId, adminUserId: 1, target: "Complainant", notes: "Please clarify");

        var updated = await sut.GetAsync(filed.ComplaintId, 99, "Student");
        Assert.Equal("AwaitingComplainant", updated!.Status);
        Assert.True(updated.RequiresComplainantResponse);
        Assert.Contains(updated.AvailableActions, a => a == "RespondAsComplainant");
    }

    [Fact]
    public async Task FileSession_MirrorsLegacyAndUnifiedRows()
    {
        await using var db = CreateDb();
        var (schedule, studentId) = await SeedCompletedSessionAsync(db);
        var sessionSut = ComplaintResolutionTestHelper.CreateComplaintService(db);

        var legacy = await sessionSut.FileComplaintAsync(
            schedule.Id, studentId, 99, SessionComplaintReason.QualityIssue, "Poor audio", null);

        Assert.Single(db.SessionComplaints.ToList());
        var unified = Assert.Single(db.Complaints.ToList());
        Assert.Equal(legacy.Id, unified.LegacySessionComplaintId);
        Assert.Equal(ComplaintStatus.Submitted, unified.Status);
    }

    [Fact]
    public async Task AssignAsync_WrongSchedule_Throws()
    {
        await using var db = CreateDb();
        var (schedule, studentId) = await SeedCompletedSessionAsync(db);
        var sut = ComplaintResolutionTestHelper.CreateComplaintService(db);

        var filed = await sut.FileComplaintAsync(
            schedule.Id, studentId, 99, SessionComplaintReason.TeacherLate, "Late", null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AssignAsync(schedule.Id + 999, filed.Id, adminUserId: 1, assignedToUserId: 2));
        Assert.Equal("Complaint does not belong to this session.", ex.Message);
    }
}
