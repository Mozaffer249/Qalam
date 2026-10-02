using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PolicyNotificationTests
{
    private const int PayerUserId = 42;
    private const int OwnerUserId = 43;

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

    private static PolicyNotificationService CreateSut(ApplicationDBContext db) => new(
        db,
        new NotificationDispatcher(
            db,
            Mock.Of<IRabbitMQService>(),
            Mock.Of<IPushNotificationService>(),
            NullLogger<NotificationDispatcher>.Instance),
        NullLogger<PolicyNotificationService>.Instance);

    private static async Task<PolicyCase> SeedCaseAsync(ApplicationDBContext db, PolicyCaseKind kind, decimal walletRefund)
    {
        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = 5,
            ApprovedAt = DateTime.UtcNow,
            AmountDue = 500m,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Cancelled,
            PaidByUserId = PayerUserId,
            OwnerUserId = OwnerUserId,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var policyCase = new PolicyCase
        {
            Kind = kind,
            EnrollmentId = enrollment.Id,
            RefundAmount = walletRefund,
            Currency = "SAR",
            Destination = RefundDestination.Wallet,
            ActorRole = "Student",
        };
        db.PolicyCases.Add(policyCase);
        await db.SaveChangesAsync();

        if (walletRefund > 0)
        {
            var credit = await ComplaintResolutionTestHelper.CreateWalletService(db).CreditAsync(new WalletEntryRequest
            {
                UserId = PayerUserId,
                Amount = walletRefund,
                Type = WalletTransactionType.Refund,
                RefundId = 9001,
                EnrollmentId = enrollment.Id,
                PolicyCaseId = policyCase.Id,
            });
            Assert.True(credit.Succeeded);
        }

        return policyCase;
    }

    [Fact]
    public async Task EnrollmentCancel_NotifiesPayerAndOwner_AndCreditsWalletOnlyForPayer()
    {
        await using var db = CreateDb();
        var policyCase = await SeedCaseAsync(db, PolicyCaseKind.BeforeFirstSessionCancel, 250m);

        await CreateSut(db).NotifyCaseAsync(policyCase);

        var rows = await db.UserNotifications.ToListAsync();
        var cancelled = rows.Where(n => n.Type == PolicyNotificationTypes.EnrollmentCancelled).ToList();
        Assert.Equal(new[] { PayerUserId, OwnerUserId }, cancelled.Select(n => n.UserId).OrderBy(x => x));
        Assert.All(cancelled, n => Assert.Contains("250 SAR", n.BodyEn));

        var credited = Assert.Single(rows, n => n.Type == PolicyNotificationTypes.WalletCredited);
        Assert.Equal(PayerUserId, credited.UserId);
        Assert.Contains($"\"policyCaseId\":{policyCase.Id}", credited.DataJson);
        Assert.All(rows, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task Reversal_SendsNothing()
    {
        await using var db = CreateDb();
        var policyCase = await SeedCaseAsync(db, PolicyCaseKind.Reversal, 0m);

        await CreateSut(db).NotifyCaseAsync(policyCase);

        Assert.Empty(await db.UserNotifications.ToListAsync());
    }

    [Fact]
    public async Task Inbox_MarkReadIsScopedToOwner()
    {
        await using var db = CreateDb();
        var policyCase = await SeedCaseAsync(db, PolicyCaseKind.BeforeFirstSessionCancel, 0m);
        await CreateSut(db).NotifyCaseAsync(policyCase);
        var inbox = new UserNotificationService(db);
        var payerRow = await db.UserNotifications.FirstAsync(n => n.UserId == PayerUserId);

        Assert.False(await inbox.MarkReadAsync(OwnerUserId, payerRow.Id));
        Assert.True(await inbox.MarkReadAsync(PayerUserId, payerRow.Id));
        Assert.Equal(0, await inbox.UnreadCountAsync(PayerUserId));
        Assert.Equal(1, await inbox.MarkAllReadAsync(OwnerUserId));

        var page = await inbox.ListAsync(PayerUserId, unreadOnly: false, page: 1, pageSize: 20);
        Assert.Single(page.Items);
        Assert.True(page.Items[0].IsRead);
    }
}
