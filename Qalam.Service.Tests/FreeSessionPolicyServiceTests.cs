using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Student;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class FreeSessionPolicyServiceTests
{
    private static ApplicationDBContext CreateDb(string? databaseName = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDBContext(options, config);
    }

    private static FreeSessionPolicyService CreateSut(ApplicationDBContext db)
    {
        return new FreeSessionPolicyService(
            db,
            new StudentRepository(db),
            new TeacherRepository(db),
            new TeacherLevelRepository(db),
            new TeacherDomainPricingRepository(db));
    }

    [Fact]
    public async Task IsStudentEligibleForFreeTrialAsync_Unused_ReturnsTrue()
    {
        await using var db = CreateDb();
        db.Students.Add(new Student { Id = 1, HasUsedFreeTrialSession = false, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        Assert.True(await sut.IsStudentEligibleForFreeTrialAsync(1));
    }

    [Fact]
    public async Task IsStudentEligibleForFreeTrialAsync_Used_ReturnsFalse()
    {
        await using var db = CreateDb();
        db.Students.Add(new Student { Id = 1, HasUsedFreeTrialSession = true, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        Assert.False(await sut.IsStudentEligibleForFreeTrialAsync(1));
    }

    [Fact]
    public void IsEligiblePackage_IndividualSingleSession_ReturnsTrue()
    {
        var sut = CreateSut(CreateDb());
        Assert.True(sut.IsEligiblePackage(isGroup: false, sessionCount: 1));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public void IsEligiblePackage_GroupOrMultiSession_ReturnsFalse(bool isGroup, int sessionCount)
    {
        var sut = CreateSut(CreateDb());
        Assert.False(sut.IsEligiblePackage(isGroup, sessionCount));
    }

    [Fact]
    public async Task ReserveThenCancelBeforeStart_RestoresEligibilityAndKeepsAuditRow()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        db.Students.Add(new Student { Id = 1, HasUsedFreeTrialSession = false, CreatedAt = now });
        var enrollment = new Enrollment
        {
            Id = 10,
            IsFreeTrial = true,
            ApprovedByTeacherId = 5,
            CreatedAt = now
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.ReserveStudentFreeTrialAsync(
            1, enrollment, FreeTrialConsumptionSource.CourseEnrollment, 5, 3, cancellationToken: default);

        var studentAfterReserve = await db.Students.FindAsync(1);
        Assert.True(studentAfterReserve!.HasUsedFreeTrialSession);

        await sut.CancelConsumptionBeforeStartAsync(10, 99, "student cancel", default);

        var consumption = await db.StudentFreeTrialConsumptions.SingleAsync();
        Assert.Equal(FreeTrialConsumptionStatus.CancelledBeforeStart, consumption.Status);
        Assert.True(consumption.RestoredEligibility);
        Assert.Equal("student cancel", consumption.CancelReason);

        var studentAfterCancel = await db.Students.FindAsync(1);
        Assert.False(studentAfterCancel!.HasUsedFreeTrialSession);
    }

    [Fact]
    public async Task MarkConsumptionConsumedAsync_SetsConsumedAndScheduleLink()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        db.Students.Add(new Student { Id = 1, HasUsedFreeTrialSession = true, CreatedAt = now });
        db.Enrollments.Add(new Enrollment { Id = 10, IsFreeTrial = true, CreatedAt = now });
        db.StudentFreeTrialConsumptions.Add(new StudentFreeTrialConsumption
        {
            StudentId = 1,
            Source = FreeTrialConsumptionSource.CourseEnrollment,
            EnrollmentId = 10,
            TeacherId = 5,
            DomainId = 3,
            Status = FreeTrialConsumptionStatus.Reserved,
            ReservedAt = now,
            CreatedAt = now
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.MarkConsumptionConsumedAsync(10, 77, default);

        var consumption = await db.StudentFreeTrialConsumptions.SingleAsync();
        Assert.Equal(FreeTrialConsumptionStatus.Consumed, consumption.Status);
        Assert.Equal(77, consumption.CourseScheduleId);
        Assert.NotNull(consumption.ConsumedAt);
    }

    [Fact]
    public async Task TryCompleteTeacherInterviewAsync_UnlocksStarterLevelWithAutoAttribution()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        db.Teachers.Add(new Teacher { Id = 5, HasCompletedInterviewSession = false, CreatedAt = now });
        db.TeacherLevels.Add(new TeacherLevel
        {
            Id = 11,
            Code = "starter",
            NameEn = "Starter",
            NameAr = "Starter",
            OrderIndex = 1,
            IsActive = true,
            TeacherSharePct = 70,
            CreatedAt = now
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.TryCompleteTeacherInterviewAsync(5, 3, enrollmentId: 10, courseScheduleId: 77, default);

        var pricing = await db.TeacherDomainPricings.SingleAsync(p => p.TeacherId == 5 && p.DomainId == 3);
        Assert.True(pricing.HasCompletedInterviewSession);
        Assert.Equal(11, pricing.TeacherLevelId);
        Assert.Equal(InterviewUnlockSource.AutoFromSession, pricing.InterviewUnlockSource);
        Assert.Equal(10, pricing.InterviewUnlockEnrollmentId);
        Assert.Equal(77, pricing.InterviewUnlockCourseScheduleId);
        Assert.NotNull(pricing.InterviewUnlockedAt);
    }

    [Fact]
    public async Task TryRevertTeacherInterviewFromEnrollmentAsync_RevertsAutoUnlockWhenSoleSource()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        db.Teachers.Add(new Teacher
        {
            Id = 5,
            HasCompletedInterviewSession = true,
            TeacherLevelId = 11,
            CreatedAt = now
        });
        db.TeacherLevels.Add(new TeacherLevel
        {
            Id = 11,
            Code = "starter",
            NameEn = "Starter",
            NameAr = "Starter",
            OrderIndex = 1,
            IsActive = true,
            TeacherSharePct = 70,
            CreatedAt = now
        });
        db.PricingSnapshots.Add(new PricingSnapshot
        {
            Id = 1,
            DomainId = 3,
            Context = PricingSnapshotContext.Enrollment,
            ContextEntityId = 10,
            SessionTypeCode = "individual",
            MarketCode = "SA",
            Currency = "SAR",
            CreatedAt = now
        });
        db.Enrollments.Add(new Enrollment
        {
            Id = 10,
            IsFreeTrial = true,
            ApprovedByTeacherId = 5,
            PricingSnapshotId = 1,
            CreatedAt = now
        });
        db.TeacherDomainPricings.Add(new TeacherDomainPricing
        {
            TeacherId = 5,
            DomainId = 3,
            TeacherLevelId = 11,
            HasCompletedInterviewSession = true,
            InterviewUnlockSource = InterviewUnlockSource.AutoFromSession,
            InterviewUnlockEnrollmentId = 10,
            InterviewUnlockCourseScheduleId = 77,
            InterviewUnlockedAt = now,
            CreatedAt = now
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.TryRevertTeacherInterviewFromEnrollmentAsync(10, default);

        var pricing = await db.TeacherDomainPricings.SingleAsync();
        Assert.False(pricing.HasCompletedInterviewSession);
        Assert.Null(pricing.TeacherLevelId);
        Assert.Equal(InterviewUnlockSource.None, pricing.InterviewUnlockSource);
        Assert.Null(pricing.InterviewUnlockEnrollmentId);
        Assert.NotNull(pricing.InterviewRevertedAt);

        var teacher = await db.Teachers.FindAsync(5);
        Assert.False(teacher!.HasCompletedInterviewSession);
        Assert.Null(teacher.TeacherLevelId);
    }

    [Fact]
    public async Task TryRevertTeacherInterviewFromEnrollmentAsync_DoesNotRevertAdminUnlock()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        db.Teachers.Add(new Teacher
        {
            Id = 5,
            HasCompletedInterviewSession = true,
            TeacherLevelId = 12,
            CreatedAt = now
        });
        db.Enrollments.Add(new Enrollment
        {
            Id = 10,
            IsFreeTrial = true,
            ApprovedByTeacherId = 5,
            CreatedAt = now
        });
        db.TeacherDomainPricings.Add(new TeacherDomainPricing
        {
            TeacherId = 5,
            DomainId = 3,
            TeacherLevelId = 12,
            HasCompletedInterviewSession = true,
            InterviewUnlockSource = InterviewUnlockSource.Admin,
            InterviewUnlockedAt = now,
            CreatedAt = now
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.TryRevertTeacherInterviewFromEnrollmentAsync(10, default);

        var pricing = await db.TeacherDomainPricings.SingleAsync();
        Assert.True(pricing.HasCompletedInterviewSession);
        Assert.Equal(InterviewUnlockSource.Admin, pricing.InterviewUnlockSource);
        Assert.Equal(12, pricing.TeacherLevelId);
        Assert.Null(pricing.InterviewRevertedAt);
    }
}
