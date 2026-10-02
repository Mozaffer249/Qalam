using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PolicyCaseReversalTests
{
    private const int PayerUserId = 42;
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

    private static PolicyCaseAdminService CreateSut(ApplicationDBContext db) => new(
        db,
        new PolicyResolver(new PolicyVersionRepository(db)),
        Mock.Of<IPolicyContextBuilder>(),
        new CancellationPolicyEngine(),
        Mock.Of<IPolicyCaseExecutor>(),
        ComplaintResolutionTestHelper.CreateWalletService(db),
        new ReplacementScheduleService(db));

    /// <summary>A case that refunded 300 to the wallet (fee 20) and clawed 50 back from the teacher.</summary>
    private static async Task<(PolicyCase Case, Refund Refund, Payment Payment)> SeedWalletRefundCaseAsync(
        ApplicationDBContext db, RefundDestination destination = RefundDestination.Wallet)
    {
        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = TeacherId,
            ApprovedAt = DateTime.UtcNow,
            AmountDue = 800m,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            OwnerUserId = PayerUserId,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var payment = new Payment
        {
            PayerUserId = PayerUserId,
            PaymentProvider = "Mock",
            Currency = "SAR",
            Subtotal = 800m,
            TotalAmount = 800m,
            Status = PaymentStatus.Succeeded,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var policyCase = new PolicyCase
        {
            Kind = PolicyCaseKind.AfterFirstSessionCancel,
            EnrollmentId = enrollment.Id,
            PaymentId = payment.Id,
            RefundAmount = 300m,
            FeeAmount = 20m,
            TeacherEarningImpact = -50m,
            PlatformRevenueImpact = -250m,
            Currency = "SAR",
            Destination = destination,
            ActorRole = "Student",
            ActorUserId = PayerUserId,
        };
        db.PolicyCases.Add(policyCase);
        await db.SaveChangesAsync();

        var refund = new Refund
        {
            PaymentId = payment.Id,
            EnrollmentId = enrollment.Id,
            Amount = 300m,
            FeeAmount = 20m,
            Currency = "SAR",
            Reason = "Policy",
            Status = RefundStatus.Succeeded,
            Destination = destination,
            PolicyCaseId = policyCase.Id,
        };
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        if (destination == RefundDestination.Wallet)
        {
            var wallets = ComplaintResolutionTestHelper.CreateWalletService(db);
            var credit = await wallets.CreditAsync(new WalletEntryRequest
            {
                UserId = PayerUserId,
                Amount = 300m,
                Type = WalletTransactionType.Refund,
                RefundId = refund.Id,
                PaymentId = payment.Id,
                EnrollmentId = enrollment.Id,
                PolicyCaseId = policyCase.Id,
            });
            Assert.True(credit.Succeeded);
        }

        db.TeacherBalanceAdjustments.Add(new TeacherBalanceAdjustment
        {
            TeacherId = TeacherId,
            Amount = 50m,
            Currency = "SAR",
            Kind = TeacherBalanceAdjustmentKind.Deduction,
            Status = TeacherBalanceAdjustmentStatus.Pending,
            ReasonCode = "POLICY_EARNING_CLAWBACK",
            PolicyCaseId = policyCase.Id,
        });
        await db.SaveChangesAsync();

        return (policyCase, refund, payment);
    }

    [Fact]
    public async Task Reverse_WalletRefund_CompensatesWalletRefundAndTeacher()
    {
        await using var db = CreateDb();
        var (original, refund, payment) = await SeedWalletRefundCaseAsync(db);
        var sut = CreateSut(db);

        var result = await sut.ReverseAsync(original.Id, "Student received the session after all", adminUserId: 1);

        Assert.NotNull(result);
        Assert.Equal("Reversal", result!.Kind);
        Assert.Equal(original.Id, result.ReversesCaseId);
        Assert.Equal(-300m, result.RefundAmount);
        Assert.Equal(50m, result.TeacherEarningImpact);

        var reloaded = await db.PolicyCases.AsNoTracking().SingleAsync(c => c.Id == original.Id);
        Assert.Equal(PolicyCaseStatus.Reversed, reloaded.Status);
        Assert.Equal(result.Id, reloaded.ReversedByCaseId);

        var wallet = await db.StudentWallets.AsNoTracking().SingleAsync(w => w.UserId == PayerUserId);
        Assert.Equal(0m, wallet.Balance);
        var txs = await db.WalletTransactions.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
        Assert.Equal(wallet.Balance, txs.Sum(t => t.Amount));
        Assert.Equal(WalletTransactionStatus.Reversed, txs[0].Status);
        Assert.Equal(WalletTransactionType.PolicyReversal, txs[1].Type);
        Assert.Equal(txs[1].BalanceBefore + txs[1].Amount, txs[1].BalanceAfter);

        var netRefunded = await db.Refunds.AsNoTracking()
            .Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Succeeded)
            .SumAsync(r => r.Amount);
        Assert.Equal(0m, netRefunded);
        Assert.Contains(db.Refunds.AsNoTracking(), r => r.Amount == -refund.Amount && r.PolicyCaseId == result.Id);

        var correction = await db.TeacherBalanceAdjustments.AsNoTracking().SingleAsync(a => a.PolicyCaseId == result.Id);
        Assert.Equal(TeacherBalanceAdjustmentKind.Correction, correction.Kind);
        Assert.Equal(-50m, correction.Amount);
    }

    [Fact]
    public async Task Reverse_Twice_IsRejected()
    {
        await using var db = CreateDb();
        var (original, _, _) = await SeedWalletRefundCaseAsync(db);
        var sut = CreateSut(db);

        await sut.ReverseAsync(original.Id, "Student received the session after all", adminUserId: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ReverseAsync(original.Id, "Second attempt at reversing", adminUserId: 1));
    }

    [Fact]
    public async Task Reverse_OriginalMethodRefund_IsRejected()
    {
        await using var db = CreateDb();
        var (original, _, _) = await SeedWalletRefundCaseAsync(db, RefundDestination.OriginalMethod);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ReverseAsync(original.Id, "Student received the session after all", adminUserId: 1));
        Assert.Contains("original payment method", ex.Message);
        Assert.Equal(PolicyCaseStatus.Applied, (await db.PolicyCases.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Reverse_ShortReason_IsRejected()
    {
        await using var db = CreateDb();
        var (original, _, _) = await SeedWalletRefundCaseAsync(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSut(db).ReverseAsync(original.Id, "short", adminUserId: 1));
    }

    [Fact]
    public async Task PayoutBatch_NetsPendingAdjustments_AndDefersOversizedDeduction()
    {
        await using var db = CreateDb();
        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = TeacherId,
            ApprovedAt = DateTime.UtcNow,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();
        db.TeacherEarningLines.Add(new TeacherEarningLine
        {
            TeacherId = TeacherId, EnrollmentId = enrollment.Id, Amount = 70m, Currency = "SAR",
            Status = TeacherEarningLineStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-5),
        });
        db.TeacherBalanceAdjustments.AddRange(
            new TeacherBalanceAdjustment { TeacherId = TeacherId, Amount = 20m, Kind = TeacherBalanceAdjustmentKind.Deduction, Status = TeacherBalanceAdjustmentStatus.Pending, ReasonCode = "A" },
            new TeacherBalanceAdjustment { TeacherId = TeacherId, Amount = -5m, Kind = TeacherBalanceAdjustmentKind.Correction, Status = TeacherBalanceAdjustmentStatus.Pending, ReasonCode = "B" },
            new TeacherBalanceAdjustment { TeacherId = TeacherId, Amount = 100m, Kind = TeacherBalanceAdjustmentKind.Deduction, Status = TeacherBalanceAdjustmentStatus.Pending, ReasonCode = "C" });
        await db.SaveChangesAsync();

        await new PayoutService(new PayoutRepository(db)).CreateBatchFromPendingAsync(null, null, 1);

        var item = await db.PayoutItems.AsNoTracking().SingleAsync();
        Assert.Equal(55m, item.Amount);
        var adjustments = await db.TeacherBalanceAdjustments.AsNoTracking().ToListAsync();
        Assert.Equal(TeacherBalanceAdjustmentStatus.Applied, adjustments.Single(a => a.ReasonCode == "A").Status);
        Assert.Equal(TeacherBalanceAdjustmentStatus.Applied, adjustments.Single(a => a.ReasonCode == "B").Status);
        Assert.Equal(TeacherBalanceAdjustmentStatus.Pending, adjustments.Single(a => a.ReasonCode == "C").Status);
    }
}
