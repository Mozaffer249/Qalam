using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class StudentWalletServiceTests
{
    private const int UserId = 42;

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

    private static StudentWalletService CreateSut(ApplicationDBContext db)
        => new(new StudentWalletRepository(db), Options.Create(new PaymentSettings()));

    private static WalletEntryRequest Entry(
        decimal amount,
        WalletTransactionType type,
        int? paymentId = null,
        int? refundId = null)
        => new() { UserId = UserId, Amount = amount, Type = type, PaymentId = paymentId, RefundId = refundId };

    [Fact]
    public async Task Credit_then_debit_updates_balance_and_ledger()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);

        var topUp = await sut.CreditAsync(Entry(100m, WalletTransactionType.TopUp, paymentId: 1));
        var pay = await sut.DebitAsync(Entry(30m, WalletTransactionType.Payment, paymentId: 2));

        Assert.True(topUp.Succeeded);
        Assert.True(pay.Succeeded);
        Assert.Equal(-30m, pay.Transaction!.Amount);
        Assert.Equal(70m, pay.Transaction.BalanceAfter);

        var wallet = await db.StudentWallets.SingleAsync(w => w.UserId == UserId);
        Assert.Equal(70m, wallet.Balance);
        Assert.Equal(wallet.Balance, await db.WalletTransactions.SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task Debit_fails_when_balance_is_insufficient()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        await sut.CreditAsync(Entry(20m, WalletTransactionType.TopUp, paymentId: 1));

        var result = await sut.DebitAsync(Entry(50m, WalletTransactionType.Payment, paymentId: 2));

        Assert.False(result.Succeeded);
        Assert.Equal(StudentWalletService.InsufficientBalance, result.ErrorCode);
        Assert.Equal(20m, (await db.StudentWallets.SingleAsync()).Balance);
        Assert.Equal(1, await db.WalletTransactions.CountAsync());
    }

    [Fact]
    public async Task Top_up_for_same_payment_is_credited_once()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);

        var first = await sut.CreditAsync(Entry(50m, WalletTransactionType.TopUp, paymentId: 7));
        var second = await sut.CreditAsync(Entry(50m, WalletTransactionType.TopUp, paymentId: 7));

        Assert.False(first.AlreadyApplied);
        Assert.True(second.AlreadyApplied);
        Assert.Equal(first.Transaction!.Id, second.Transaction!.Id);
        Assert.Equal(50m, (await db.StudentWallets.SingleAsync()).Balance);
    }

    [Fact]
    public async Task Refund_for_same_refund_id_is_credited_once()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);

        await sut.CreditAsync(Entry(25m, WalletTransactionType.Refund, paymentId: 3, refundId: 11));
        await sut.CreditAsync(Entry(25m, WalletTransactionType.Refund, paymentId: 3, refundId: 11));

        Assert.Equal(25m, (await db.StudentWallets.SingleAsync()).Balance);
    }

    [Fact]
    public async Task Rejects_non_positive_amounts()
    {
        using var db = CreateDb();
        var result = await CreateSut(db).CreditAsync(Entry(0m, WalletTransactionType.AdminCredit));

        Assert.False(result.Succeeded);
        Assert.Equal(StudentWalletService.InvalidAmount, result.ErrorCode);
    }

    [Fact]
    public async Task Summary_totals_and_filters_group_transaction_types()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        await sut.CreditAsync(Entry(100m, WalletTransactionType.TopUp, paymentId: 1));
        await sut.CreditAsync(Entry(10m, WalletTransactionType.AdminCredit));
        await sut.DebitAsync(Entry(60m, WalletTransactionType.Payment, paymentId: 2));
        await sut.CreditAsync(Entry(60m, WalletTransactionType.Reversal, paymentId: 2));
        await sut.DebitAsync(Entry(40m, WalletTransactionType.Payment, paymentId: 3));
        await sut.CreditAsync(Entry(15m, WalletTransactionType.Refund, paymentId: 3, refundId: 9));

        var summary = await sut.GetSummaryAsync(UserId);

        Assert.Equal(85m, summary.Balance);
        Assert.Equal(110m, summary.TotalAdded);
        Assert.Equal(40m, summary.TotalSpent);
        Assert.Equal(15m, summary.TotalRefunded);

        var (added, _) = await sut.ListTransactionsAsync(UserId, "added", 1, 20);
        var (spent, _) = await sut.ListTransactionsAsync(UserId, "spent", 1, 20);
        var (refunded, _) = await sut.ListTransactionsAsync(UserId, "refunded", 1, 20);
        var (all, total) = await sut.ListTransactionsAsync(UserId, null, 1, 20);

        Assert.Equal(2, added.Count);
        Assert.Equal(3, spent.Count);
        Assert.Single(refunded);
        Assert.Equal(6, total);
        Assert.Equal(6, all.Count);
    }

    [Fact]
    public async Task Refund_defaults_to_wallet_and_skips_gateway()
    {
        var payment = new Payment
        {
            Id = 10,
            PayerUserId = UserId,
            TotalAmount = 100m,
            Currency = "SAR",
            Status = PaymentStatus.Succeeded,
            PaymentProvider = "Moyasar",
            ProviderTransactionId = "pay_recorded",
            Refunds = new List<Refund>()
        };

        var refunds = new Mock<IRefundRepository>();
        refunds.Setup(r => r.GetTrackedPaymentWithRefundsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        refunds.Setup(r => r.AddRefundAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()))
            .Callback<Refund, CancellationToken>((r, _) => r.Id = 77)
            .Returns(Task.CompletedTask);
        refunds.Setup(r => r.GetEnrollmentPaymentsForPaymentAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentPayment>());
        refunds.Setup(r => r.GetPendingEarningLinesForEnrollmentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeacherEarningLine>());
        refunds.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var finance = new Mock<ITeacherFinanceImpactService>();
        finance.Setup(f => f.IsAlreadyPaidForEnrollmentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var moyasar = new Mock<IPaymentGateway>();
        moyasar.SetupGet(g => g.ProviderName).Returns("Moyasar");
        moyasar.SetupGet(g => g.IsConfigured).Returns(true);

        var wallet = new Mock<IStudentWalletService>();
        wallet.Setup(w => w.CreditAsync(It.IsAny<WalletEntryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WalletOperationResult.Ok(new WalletTransaction { Id = 5 }));

        var resolver = new PaymentGatewayResolver(
            new IPaymentGateway[] { new MockPaymentGateway(), moyasar.Object },
            ComplaintResolutionTestHelperSettings());
        var service = new RefundService(
            refunds.Object, finance.Object, resolver, Mock.Of<IPaymentTransactionEventService>(), wallet.Object);

        var refund = await service.IssueRefundAsync(
            paymentId: 10,
            enrollmentId: 1,
            amount: 40m,
            currency: "SAR",
            reason: "cancelled",
            initiatedByUserId: null,
            courseScheduleId: 300);

        Assert.Equal(RefundStatus.Succeeded, refund.Status);
        Assert.Equal(RefundDestination.Wallet, refund.Destination);
        Assert.Equal("WALLET-5", refund.ProviderRefundId);
        wallet.Verify(w => w.CreditAsync(
            It.Is<WalletEntryRequest>(e =>
                e.UserId == UserId &&
                e.Amount == 40m &&
                e.Type == WalletTransactionType.Refund &&
                e.RefundId == 77 &&
                e.PaymentId == 10 &&
                e.CourseScheduleId == 300),
            It.IsAny<CancellationToken>()), Times.Once);
        moyasar.Verify(g => g.RefundAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static IPaymentGatewaySettingsProvider ComplaintResolutionTestHelperSettings()
    {
        var settings = new Mock<IPaymentGatewaySettingsProvider>();
        settings.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Qalam.Data.DTOs.Platform.PaymentGatewaySettingsDto
            {
                ActiveProvider = MockPaymentGateway.Name
            });
        return settings.Object;
    }
}
