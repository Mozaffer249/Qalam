using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PayoutServiceFsmTests
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

    private static async Task<TeacherEarningLine> SeedPendingLineAsync(ApplicationDBContext db)
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
            ApprovedByTeacherId = TeacherId,
            ApprovedAt = DateTime.UtcNow,
            AmountDue = 100m,
            PricingSnapshotId = snapshot.Id,
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
            Source = TeacherEarningSource.SessionCompleted,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
        };
        db.TeacherEarningLines.Add(line);
        await db.SaveChangesAsync();
        return line;
    }

    private static PayoutService CreateSut(ApplicationDBContext db) =>
        new(new PayoutRepository(db));

    [Fact]
    public async Task FullFlow_Pending_Approved_Processing_Paid()
    {
        await using var db = CreateDb();
        await SeedPendingLineAsync(db);
        var sut = CreateSut(db);

        var batch = await sut.CreateBatchFromPendingAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            createdByUserId: 1);
        Assert.Equal("Pending", batch.Status);

        var approved = await sut.ApproveAsync(batch.Id, approvedByUserId: 1);
        Assert.Equal("Approved", approved!.Status);

        var processing = await sut.ProcessAsync(batch.Id, processedByUserId: 1);
        Assert.Equal("Processing", processing!.Status);

        var paid = await sut.MarkPaidAsync(batch.Id);
        Assert.Equal("Paid", paid!.Status);
        Assert.False(string.IsNullOrWhiteSpace(paid.MockTransferRef));
    }

    [Fact]
    public async Task Reject_ReleasesLinesBackToPending()
    {
        await using var db = CreateDb();
        var line = await SeedPendingLineAsync(db);
        var sut = CreateSut(db);

        var batch = await sut.CreateBatchFromPendingAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            createdByUserId: 1);

        var rejected = await sut.RejectAsync(batch.Id, "Invalid period");
        Assert.Equal("Rejected", rejected!.Status);

        var refreshed = await db.TeacherEarningLines.FindAsync(line.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(TeacherEarningLineStatus.Pending, refreshed!.Status);
        Assert.Null(refreshed.PayoutItemId);
    }

    [Fact]
    public async Task Cancel_FromApproved_ReleasesLines()
    {
        await using var db = CreateDb();
        var line = await SeedPendingLineAsync(db);
        var sut = CreateSut(db);

        var batch = await sut.CreateBatchFromPendingAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            createdByUserId: 1);
        await sut.ApproveAsync(batch.Id);

        var cancelled = await sut.CancelAsync(batch.Id, "Admin cancelled");
        Assert.Equal("Cancelled", cancelled!.Status);

        var refreshed = await db.TeacherEarningLines.FindAsync(line.Id);
        Assert.Equal(TeacherEarningLineStatus.Pending, refreshed!.Status);
    }

    [Fact]
    public async Task MarkPaid_FromApproved_Throws()
    {
        await using var db = CreateDb();
        await SeedPendingLineAsync(db);
        var sut = CreateSut(db);

        var batch = await sut.CreateBatchFromPendingAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            createdByUserId: 1);
        await sut.ApproveAsync(batch.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.MarkPaidAsync(batch.Id));
    }

    [Fact]
    public async Task Failed_ThenRetry_ReturnsToProcessing()
    {
        await using var db = CreateDb();
        await SeedPendingLineAsync(db);
        var sut = CreateSut(db);

        var batch = await sut.CreateBatchFromPendingAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            createdByUserId: 1);
        await sut.ApproveAsync(batch.Id);
        await sut.ProcessAsync(batch.Id);

        var failed = await sut.MarkFailedAsync(batch.Id, "Bank timeout");
        Assert.Equal("Failed", failed!.Status);

        var retried = await sut.RetryAsync(batch.Id, processedByUserId: 1);
        Assert.Equal("Processing", retried!.Status);
        Assert.Null(retried.FailureReason);
    }
}
