using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Pricing;
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
}
