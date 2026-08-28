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

public class AdminEnrollmentDetailTests
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

    private static async Task SeedStarterLevelAsync(ApplicationDBContext db, decimal sharePct = 10m)
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
    public async Task GetById_IncludesPaymentMethodSessionsAndAmountRemaining()
    {
        await using var db = CreateDb();

        var snapshot = new PricingSnapshot
        {
            PricePerHour = 85m,
            EarningsPricePerHour = 59.5m,
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
            ApprovedByTeacherId = 5,
            ApprovedAt = DateTime.UtcNow,
            IsFreeTrial = true,
            AmountDue = 85m,
            PaidByUserId = 1,
            PricingSnapshotId = snapshot.Id,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var participant = new EnrollmentParticipant
        {
            EnrollmentId = enrollment.Id,
            StudentId = 1,
            PaymentStatus = PaymentStatus.Succeeded,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.EnrollmentParticipants.Add(participant);
        await db.SaveChangesAsync();

        db.CourseSchedules.AddRange(
            new CourseSchedule
            {
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                DurationMinutes = 60,
                Status = ScheduleStatus.Scheduled,
                TeacherAvailabilityId = 1,
                TeachingModeId = 1,
                CreatedAt = DateTime.UtcNow,
            },
            new CourseSchedule
            {
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
                DurationMinutes = 60,
                Status = ScheduleStatus.Scheduled,
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
            Subtotal = 85m,
            VatAmount = 0m,
            DiscountAmount = 0m,
            TotalAmount = 85m,
            InvoiceNumber = "INV-001",
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        db.EnrollmentPayments.Add(new EnrollmentPayment
        {
            EnrollmentParticipantId = participant.Id,
            PaymentId = payment.Id,
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var levelRepo = new TeacherLevelRepository(db);
        var sut = new AdminEnrollmentQueryService(db, levelRepo);

        Assert.Equal(2, db.CourseSchedules.Count(s => s.EnrollmentId == enrollment.Id));

        var detail = await sut.GetByIdAsync(enrollment.Id);

        Assert.NotNull(detail);
        Assert.Equal("MOCK", detail!.PaymentMethod);
        Assert.Equal(85m, detail.AmountPaid);
        Assert.Equal(0m, detail.AmountRemaining);
        Assert.Single(detail.Payments);
        Assert.Equal("MOCK", detail.Payments[0].Provider);
        Assert.Equal("SA", detail.SnapshotMarketCode);
        Assert.Equal(59.5m, detail.SnapshotEarningsPricePerHour);
        Assert.Equal("individual", detail.SnapshotSessionTypeCode);
    }

    [Fact]
    public async Task GetById_InterviewPending_IncludesAccruedEarningsAndSessionAccruals()
    {
        await using var db = CreateDb();
        await SeedStarterLevelAsync(db, sharePct: 10m);

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
            ApprovedByTeacherId = 5,
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

        var freeSchedule = new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Enrollment = enrollment,
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
            Enrollment = enrollment,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
            DurationMinutes = 60,
            Status = ScheduleStatus.Completed,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        db.CourseSchedules.AddRange(freeSchedule, paidSchedule);
        await db.SaveChangesAsync();

        const decimal accruedAmount = 8.50m;
        db.TeacherEarningLines.Add(new TeacherEarningLine
        {
            TeacherId = 5,
            EnrollmentId = enrollment.Id,
            CourseScheduleId = paidSchedule.Id,
            Amount = accruedAmount,
            Currency = "SAR",
            Source = TeacherEarningSource.SessionCompleted,
            Status = TeacherEarningLineStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var levelRepo = new TeacherLevelRepository(db);
        var sut = new AdminEnrollmentQueryService(db, levelRepo);

        var detail = await sut.GetByIdAsync(enrollment.Id);

        Assert.NotNull(detail);
        Assert.True(detail!.IsInterviewPendingAtQuote);
        Assert.Equal(accruedAmount, detail.AccruedNet);
        Assert.Equal(accruedAmount, detail.PackageTeacherDue);
        Assert.Equal(0m, detail.RemainingToAccrue);
        Assert.Equal("Available", detail.EnrollmentEarningUiStatus);
        Assert.Equal(2, detail.Sessions.Count);

        var session1 = detail.Sessions[0];
        Assert.True(session1.IsFreeSession);
        Assert.Null(session1.AccruedAmount);
        Assert.Null(session1.EarningLineKey);

        var session2 = detail.Sessions[1];
        Assert.False(session2.IsFreeSession);
        Assert.Equal(accruedAmount, session2.AccruedAmount);
        Assert.NotNull(session2.EarningLineKey);
        Assert.StartsWith("earn-", session2.EarningLineKey);

        Assert.Single(detail.EarningLines);
        Assert.Equal(accruedAmount, detail.EarningLines[0].Amount);
    }
}
