using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Pricing;
using Qalam.Service.Implementations;
using Qalam.Infrastructure.context;

namespace Qalam.Service.Tests;

public class ComplaintResolutionOrchestratorTests
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

    private static async Task<(CourseSchedule schedule, int studentId, int complaintId, int paymentId)> SeedComplaintWithPaymentAsync(
        ApplicationDBContext db)
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

        var participant = new EnrollmentParticipant
        {
            EnrollmentId = enrollment.Id,
            StudentId = 42,
            PaymentStatus = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        };
        db.EnrollmentParticipants.Add(participant);
        await db.SaveChangesAsync();

        var payment = new Payment
        {
            PayerUserId = 1,
            PaymentProvider = "Mock",
            Subtotal = 100m,
            VatAmount = 0m,
            TotalAmount = 100m,
            Currency = "SAR",
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        db.EnrollmentPayments.Add(new EnrollmentPayment
        {
            PaymentId = payment.Id,
            EnrollmentParticipantId = participant.Id,
            Status = PaymentStatus.Succeeded,
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

        var earning = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);
        await earning.AccrueForCompletedScheduleAsync(schedule.Id, TeacherEarningLineStatus.OnHold);

        var complaint = new SessionComplaint
        {
            CourseScheduleId = schedule.Id,
            EnrollmentId = enrollment.Id,
            StudentId = 42,
            TeacherId = 5,
            ReasonCode = SessionComplaintReason.TeacherNoShow,
            Description = "No show",
            Status = SessionComplaintStatus.Open,
            FiledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.SessionComplaints.Add(complaint);
        await db.SaveChangesAsync();

        return (schedule, 42, complaint.Id, payment.Id);
    }

    [Fact]
    public async Task FullRefund_VoidsSessionLineAndIssuesSessionProratedRefund()
    {
        await using var db = CreateDb();
        var (schedule, _, complaintId, paymentId) = await SeedComplaintWithPaymentAsync(db);
        var sut = ComplaintResolutionTestHelper.CreateOrchestrator(db);

        await sut.ResolveAsync(
            schedule.Id,
            complaintId,
            adminUserId: 1,
            SessionComplaintResolution.FullRefund,
            resolutionNotes: "Full refund",
            refundAmountOverride: null,
            paymentIdOverride: null);

        var complaint = await db.SessionComplaints.FindAsync(complaintId);
        Assert.Equal(SessionComplaintStatus.Resolved, complaint!.Status);
        Assert.NotNull(complaint.RefundId);

        var refund = await db.Refunds.FindAsync(complaint.RefundId);
        Assert.NotNull(refund);
        Assert.Equal(100m, refund!.Amount);

        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(TeacherEarningLineStatus.Voided, line.Status);
    }

    [Fact]
    public async Task PartialRefund_UsesAdminOverrideAmount()
    {
        await using var db = CreateDb();
        var (schedule, _, complaintId, paymentId) = await SeedComplaintWithPaymentAsync(db);
        var sut = ComplaintResolutionTestHelper.CreateOrchestrator(db);

        await sut.ResolveAsync(
            schedule.Id,
            complaintId,
            adminUserId: 1,
            SessionComplaintResolution.PartialRefund,
            resolutionNotes: "Partial",
            refundAmountOverride: 40m,
            paymentIdOverride: paymentId);

        var complaint = await db.SessionComplaints.FindAsync(complaintId);
        var refund = await db.Refunds.FindAsync(complaint!.RefundId);
        Assert.Equal(40m, refund!.Amount);
    }

    [Fact]
    public async Task ReplacementSession_CreatesScheduledReplacementAndVoidsEarning()
    {
        await using var db = CreateDb();
        var (schedule, _, complaintId, _) = await SeedComplaintWithPaymentAsync(db);
        var sut = ComplaintResolutionTestHelper.CreateOrchestrator(db);

        await sut.ResolveAsync(
            schedule.Id,
            complaintId,
            adminUserId: 1,
            SessionComplaintResolution.ReplacementSession,
            resolutionNotes: "Replacement granted",
            refundAmountOverride: null,
            paymentIdOverride: null);

        var complaint = await db.SessionComplaints.FindAsync(complaintId);
        Assert.NotNull(complaint!.ReplacementScheduleId);

        var replacement = await db.CourseSchedules.FindAsync(complaint.ReplacementScheduleId);
        Assert.NotNull(replacement);
        Assert.Equal(ScheduleStatus.Scheduled, replacement!.Status);
        Assert.Equal(schedule.EnrollmentId, replacement.EnrollmentId);

        var line = Assert.Single(db.TeacherEarningLines.Where(l => l.CourseScheduleId == schedule.Id));
        Assert.Equal(TeacherEarningLineStatus.Voided, line.Status);

        Assert.Contains(
            db.SessionAuditLogs.ToList(),
            l => l.ActionType == SessionAuditActionType.ReplacementSessionGranted);
    }

    [Fact]
    public async Task WarnTeacher_LogsTeacherWarnedAndReleasesEarning()
    {
        await using var db = CreateDb();
        var (schedule, _, complaintId, _) = await SeedComplaintWithPaymentAsync(db);
        var sut = ComplaintResolutionTestHelper.CreateOrchestrator(db);

        await sut.ResolveAsync(
            schedule.Id,
            complaintId,
            adminUserId: 1,
            SessionComplaintResolution.WarnTeacher,
            resolutionNotes: "First warning",
            refundAmountOverride: null,
            paymentIdOverride: null);

        Assert.Contains(
            db.SessionAuditLogs.ToList(),
            l => l.ActionType == SessionAuditActionType.TeacherWarned);

        var warning = Assert.Single(db.TeacherDisciplinaryRecords.ToList());
        Assert.Equal(TeacherDisciplinaryKind.Warning, warning.Kind);
        Assert.Equal(complaintId, warning.ComplaintId);

        var line = Assert.Single(db.TeacherEarningLines.ToList());
        Assert.Equal(TeacherEarningLineStatus.Pending, line.Status);
    }

    [Fact]
    public async Task RefundClawback_VoidsOnHoldLines()
    {
        await using var db = CreateDb();
        var (schedule, _, complaintId, paymentId) = await SeedComplaintWithPaymentAsync(db);

        var enrollmentId = schedule.EnrollmentId;
        db.TeacherEarningLines.Add(new TeacherEarningLine
        {
            EnrollmentId = enrollmentId,
            CourseScheduleId = null,
            Amount = 25m,
            Status = TeacherEarningLineStatus.OnHold,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();

        var sut = ComplaintResolutionTestHelper.CreateOrchestrator(db);
        await sut.ResolveAsync(
            schedule.Id,
            complaintId,
            adminUserId: 1,
            SessionComplaintResolution.FullRefund,
            resolutionNotes: "Refund",
            refundAmountOverride: null,
            paymentIdOverride: paymentId);

        Assert.All(
            db.TeacherEarningLines.ToList(),
            l => Assert.Equal(TeacherEarningLineStatus.Voided, l.Status));
    }

    [Fact]
    public async Task GetPreview_FullRefund_SuggestsSessionProratedAmount()
    {
        await using var db = CreateDb();
        var (schedule, _, complaintId, paymentId) = await SeedComplaintWithPaymentAsync(db);
        var sut = ComplaintResolutionTestHelper.CreateOrchestrator(db);

        var preview = await sut.GetPreviewAsync(
            schedule.Id,
            complaintId,
            SessionComplaintResolution.FullRefund,
            refundAmountOverride: null,
            paymentIdOverride: null);

        Assert.Equal(100m, preview.SuggestedRefundAmount);
        Assert.Equal(paymentId, preview.PaymentId);
        Assert.Equal("Void", preview.SessionEarningEffect);
    }
}
