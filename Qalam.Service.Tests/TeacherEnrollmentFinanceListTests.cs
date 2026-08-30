using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Implementations;
using Qalam.Service.Repositories;

namespace Qalam.Service.Tests;

public class TeacherEnrollmentFinanceListTests
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

    private static TeacherEnrollmentFinanceListBuilder CreateListBuilder(ApplicationDBContext db)
    {
        var levelRepo = new TeacherLevelRepository(db);
        return new TeacherEnrollmentFinanceListBuilder(db, levelRepo, new TeacherLedgerReadRepository(db));
    }

    private static TeacherFinanceDetailService CreateDetailService(ApplicationDBContext db)
    {
        var levelRepo = new TeacherLevelRepository(db);
        var ledger = new TeacherLedgerReadRepository(db);
        return new TeacherFinanceDetailService(db, levelRepo, new TeacherEnrollmentFinanceListBuilder(db, levelRepo, ledger));
    }

    private static async Task<Enrollment> SeedEnrollmentAsync(ApplicationDBContext db, string? courseTitle = null)
    {
        Course? course = null;
        if (courseTitle != null)
        {
            course = new Course
            {
                Title = courseTitle,
                TeacherId = TeacherId,
                CreatedAt = DateTime.UtcNow,
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
        }

        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = TeacherId,
            ApprovedAt = DateTime.UtcNow,
            CourseId = course?.Id,
            AmountDue = 100m,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();
        return enrollment;
    }

    [Fact]
    public async Task BuildAsync_TwoSessionLinesSameEnrollment_EmitsSingleEnrollmentRow()
    {
        await using var db = CreateDb();
        var enrollment = await SeedEnrollmentAsync(db, "Math course");

        db.CourseSchedules.AddRange(
            new CourseSchedule
            {
                EnrollmentId = enrollment.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                DurationMinutes = 60,
                Status = ScheduleStatus.Completed,
                TeacherAvailabilityId = 1,
                TeachingModeId = 1,
                CreatedAt = DateTime.UtcNow,
            },
            new CourseSchedule
            {
                EnrollmentId = enrollment.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
                DurationMinutes = 60,
                Status = ScheduleStatus.Completed,
                TeacherAvailabilityId = 1,
                TeachingModeId = 1,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        db.TeacherEarningLines.AddRange(
            new TeacherEarningLine
            {
                TeacherId = TeacherId,
                EnrollmentId = enrollment.Id,
                Amount = 8.50m,
                Currency = "SAR",
                Status = TeacherEarningLineStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
            },
            new TeacherEarningLine
            {
                TeacherId = TeacherId,
                EnrollmentId = enrollment.Id,
                Amount = 8.50m,
                Currency = "SAR",
                Status = TeacherEarningLineStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        var sut = CreateListBuilder(db);
        var rows = await sut.BuildAsync(TeacherId, null, null);

        var enrollmentRows = rows.Where(r => r.Id == $"enr-{enrollment.Id}").ToList();
        Assert.Single(enrollmentRows);
        Assert.Equal("EnrollmentRevenue", enrollmentRows[0].Type);
        Assert.Equal(17.00m, enrollmentRows[0].Amount);
        Assert.DoesNotContain(rows, r => r.Id.StartsWith("earn-", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildAsync_VoidedAndRefundSameEnrollment_EmitsSingleNetRow()
    {
        await using var db = CreateDb();
        var enrollment = await SeedEnrollmentAsync(db);

        var voidedLine = new TeacherEarningLine
        {
            TeacherId = TeacherId,
            EnrollmentId = enrollment.Id,
            Amount = 8.50m,
            Currency = "SAR",
            Status = TeacherEarningLineStatus.Voided,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
        };
        db.TeacherEarningLines.Add(voidedLine);
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

        db.Refunds.Add(new Refund
        {
            PaymentId = payment.Id,
            EnrollmentId = enrollment.Id,
            Amount = 85m,
            Currency = "SAR",
            Reason = "Full refund",
            Status = RefundStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var sut = CreateListBuilder(db);
        var rows = await sut.BuildAsync(TeacherId, null, null);

        var enrollmentRow = Assert.Single(rows, r => r.Id == $"enr-{enrollment.Id}");
        Assert.Equal(-93.50m, enrollmentRow.Amount);
        Assert.DoesNotContain(rows, r => r.Type == "Refund");
        Assert.DoesNotContain(rows, r => r.Type == "Deduction");
    }

    [Fact]
    public async Task BuildAsync_PaidPayout_RemainsSeparateRow()
    {
        await using var db = CreateDb();
        var enrollment = await SeedEnrollmentAsync(db);

        db.TeacherEarningLines.Add(new TeacherEarningLine
        {
            TeacherId = TeacherId,
            EnrollmentId = enrollment.Id,
            Amount = 10m,
            Currency = "SAR",
            Status = TeacherEarningLineStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        });

        var batch = new PayoutBatch
        {
            PeriodStart = DateTime.UtcNow.AddDays(-7),
            PeriodEnd = DateTime.UtcNow,
            TotalAmount = 50m,
            Currency = "SAR",
            Status = PayoutBatchStatus.Paid,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.PayoutBatches.Add(batch);
        await db.SaveChangesAsync();

        db.PayoutItems.Add(new PayoutItem
        {
            PayoutBatchId = batch.Id,
            TeacherId = TeacherId,
            Amount = 50m,
            Currency = "SAR",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var sut = CreateListBuilder(db);
        var rows = await sut.BuildAsync(TeacherId, null, null);

        Assert.Contains(rows, r => r.Id.StartsWith("payout-", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rows, r => r.Id == $"enr-{enrollment.Id}");
    }

    [Fact]
    public async Task GetTransactionDetail_Enrollment_ReturnsEnrollmentEarnings()
    {
        await using var db = CreateDb();
        var enrollment = await SeedEnrollmentAsync(db, "Science");

        db.TeacherEarningLines.AddRange(
            new TeacherEarningLine
            {
                TeacherId = TeacherId,
                EnrollmentId = enrollment.Id,
                Amount = 5m,
                Currency = "SAR",
                Status = TeacherEarningLineStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
            },
            new TeacherEarningLine
            {
                TeacherId = TeacherId,
                EnrollmentId = enrollment.Id,
                Amount = 5m,
                Currency = "SAR",
                Status = TeacherEarningLineStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        var sut = CreateDetailService(db);
        var detail = await sut.GetTransactionDetailAsync(TeacherId, $"enr-{enrollment.Id}");

        Assert.NotNull(detail);
        Assert.Equal("EnrollmentRevenue", detail!.Type);
        Assert.Equal(10m, detail.Amount);
        Assert.NotNull(detail.EnrollmentEarnings);
        Assert.Equal(2, detail.EnrollmentEarnings!.EarningLines.Count);
    }

    [Fact]
    public async Task BuildAsync_PaymentFilter_ReturnsEnrollmentRevenueRowsOnly()
    {
        await using var db = CreateDb();
        var enrollment = await SeedEnrollmentAsync(db);

        db.TeacherEarningLines.Add(new TeacherEarningLine
        {
            TeacherId = TeacherId,
            EnrollmentId = enrollment.Id,
            Amount = 12m,
            Currency = "SAR",
            Status = TeacherEarningLineStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        });

        var batch = new PayoutBatch
        {
            PeriodStart = DateTime.UtcNow.AddDays(-7),
            PeriodEnd = DateTime.UtcNow,
            TotalAmount = 50m,
            Currency = "SAR",
            Status = PayoutBatchStatus.Paid,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.PayoutBatches.Add(batch);
        await db.SaveChangesAsync();

        db.PayoutItems.Add(new PayoutItem
        {
            PayoutBatchId = batch.Id,
            TeacherId = TeacherId,
            Amount = 50m,
            Currency = "SAR",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var sut = CreateListBuilder(db);
        var rows = await sut.BuildAsync(TeacherId, null, "Payment");

        Assert.All(rows, r => Assert.Equal("EnrollmentRevenue", r.Type));
        Assert.DoesNotContain(rows, r => r.Type == "Payout");
    }
}
