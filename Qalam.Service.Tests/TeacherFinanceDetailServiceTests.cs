using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Pricing;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class TeacherFinanceDetailServiceTests
{
    private const int TeacherId = 5;

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

    private static TeacherFinanceDetailService CreateSut(ApplicationDBContext db) =>
        new(db, new TeacherLevelRepository(db));

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
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTransactionDetail_Earning_ReturnsBreakdown_WhenOwned()
    {
        await using var db = CreateDb();
        await SeedStarterLevelAsync(db);

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
            CreatedAt = DateTime.UtcNow,
        };
        db.PricingSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = TeacherId,
            ApprovedAt = DateTime.UtcNow,
            IsFreeTrial = true,
            AmountDue = 85m,
            PricingSnapshotId = snapshot.Id,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var firstSchedule = new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DurationMinutes = 60,
            Status = ScheduleStatus.Completed,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        var paidSchedule = new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
            DurationMinutes = 60,
            Status = ScheduleStatus.Completed,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        db.CourseSchedules.AddRange(firstSchedule, paidSchedule);
        await db.SaveChangesAsync();

        var line = new TeacherEarningLine
        {
            TeacherId = TeacherId,
            EnrollmentId = enrollment.Id,
            CourseScheduleId = paidSchedule.Id,
            Amount = 59.5m,
            Currency = "SAR",
            Source = TeacherEarningSource.SessionCompleted,
            Status = TeacherEarningLineStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        db.TeacherEarningLines.Add(line);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.CourseSchedules.CountAsync(s => s.EnrollmentId == enrollment.Id));

        var sut = CreateSut(db);
        var detail = await sut.GetTransactionDetailAsync(TeacherId, $"earn-{line.Id}");

        Assert.NotNull(detail);
        Assert.Equal("Payment", detail!.Type);
        Assert.Equal(59.5m, detail.Amount);
        Assert.Equal(enrollment.Id, detail.EnrollmentId);
        Assert.NotNull(detail.Pricing);
        Assert.True(detail.Pricing!.IsInterviewPendingAtQuote);
        Assert.NotNull(detail.Projection);
        Assert.Equal(70m, detail.Projection!.ProjectedTeacherSharePct);
        Assert.NotNull(detail.Calculation);
        Assert.Equal(59.5m, detail.Calculation!.ProratedAmount);
        Assert.NotNull(detail.Session);
        Assert.False(detail.Session!.IsFreeSession);
        Assert.NotNull(detail.EnrollmentEarnings);
        Assert.Equal(2, detail.EnrollmentEarnings!.Sessions.Count);
        Assert.True(detail.EnrollmentEarnings.Sessions[0].IsFreeSession);
        Assert.Null(detail.EnrollmentEarnings.Sessions[0].AccruedAmount);
        Assert.Equal(59.5m, detail.EnrollmentEarnings.Sessions[1].AccruedAmount);
        Assert.True(detail.EnrollmentEarnings.Sessions[1].IsHighlighted);
        Assert.Equal(59.5m, detail.EnrollmentEarnings.AccruedNet);
        Assert.Equal(59.5m, detail.EnrollmentEarnings.PackageTeacherDue);
        Assert.Equal(0m, detail.EnrollmentEarnings.RemainingToAccrue);
        Assert.Single(detail.EnrollmentEarnings.EarningLines);
    }

    [Fact]
    public async Task GetTransactionDetail_Earning_ReturnsNull_WhenWrongTeacher()
    {
        await using var db = CreateDb();

        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = TeacherId,
            ApprovedAt = DateTime.UtcNow,
            AmountDue = 100m,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var line = new TeacherEarningLine
        {
            TeacherId = TeacherId,
            EnrollmentId = enrollment.Id,
            Amount = 70m,
            Currency = "SAR",
            Status = TeacherEarningLineStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        db.TeacherEarningLines.Add(line);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var detail = await sut.GetTransactionDetailAsync(99, $"earn-{line.Id}");

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetTransactionDetail_Refund_ReturnsBreakdown_WhenOwned()
    {
        await using var db = CreateDb();

        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = TeacherId,
            ApprovedAt = DateTime.UtcNow,
            AmountDue = 100m,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        db.CourseSchedules.Add(new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DurationMinutes = 60,
            Status = ScheduleStatus.Completed,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var payment = new Payment
        {
            PayerUserId = 1,
            Currency = "SAR",
            PaymentProvider = "MOCK",
            Subtotal = 100m,
            VatAmount = 0m,
            DiscountAmount = 0m,
            TotalAmount = 100m,
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var refund = new Refund
        {
            PaymentId = payment.Id,
            EnrollmentId = enrollment.Id,
            Amount = 50m,
            Currency = "SAR",
            Reason = "Partial refund",
            Status = RefundStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        };
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var detail = await sut.GetTransactionDetailAsync(TeacherId, $"ref-{refund.Id}");

        Assert.NotNull(detail);
        Assert.Equal("Refund", detail!.Type);
        Assert.Equal(-50m, detail.Amount);
        Assert.NotNull(detail.Refund);
        Assert.Equal(1, detail.Refund!.SessionsUsed);
        Assert.Equal(0, detail.Refund.SessionsUnused);
    }

    [Fact]
    public async Task GetTransactionDetail_Payout_ReturnsBreakdown_WhenOwned()
    {
        await using var db = CreateDb();

        var batch = new PayoutBatch
        {
            PeriodStart = DateTime.UtcNow.AddDays(-30),
            PeriodEnd = DateTime.UtcNow,
            TotalAmount = 70m,
            Currency = "SAR",
            Status = PayoutBatchStatus.Paid,
            MockTransferRef = "MOCK-TRX-001",
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.PayoutBatches.Add(batch);
        await db.SaveChangesAsync();

        var item = new PayoutItem
        {
            PayoutBatchId = batch.Id,
            TeacherId = TeacherId,
            Amount = 70m,
            Currency = "SAR",
            CreatedAt = DateTime.UtcNow,
        };
        db.PayoutItems.Add(item);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var detail = await sut.GetTransactionDetailAsync(TeacherId, $"payout-{item.Id}");

        Assert.NotNull(detail);
        Assert.Equal("Payout", detail!.Type);
        Assert.Equal(70m, detail.Amount);
        Assert.NotNull(detail.Payout);
        Assert.Equal("MOCK-TRX-001", detail.Payout!.MockTransferRef);
    }

    [Fact]
    public async Task GetTransactionDetail_ReturnsNull_ForInvalidKey()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db);

        Assert.Null(await sut.GetTransactionDetailAsync(TeacherId, "invalid-key"));
        Assert.Null(await sut.GetTransactionDetailAsync(TeacherId, "earn-notanumber"));
    }
}
