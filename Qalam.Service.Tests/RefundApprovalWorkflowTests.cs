using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class RefundApprovalWorkflowTests
{
    private static (RefundService Service, Mock<IPaymentGateway> Gateway, List<Refund> Store) BuildService()
    {
        var payment = new Payment
        {
            Id = 10,
            TotalAmount = 100m,
            Currency = "SAR",
            Status = PaymentStatus.Succeeded,
            PaymentProvider = "Moyasar",
            ProviderTransactionId = "pay_recorded",
            Refunds = new List<Refund>()
        };

        var store = new List<Refund>();
        var refunds = new Mock<IRefundRepository>();
        refunds
            .Setup(r => r.GetTrackedPaymentWithRefundsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        refunds
            .Setup(r => r.AddRefundAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()))
            .Returns((Refund r, CancellationToken _) =>
            {
                if (r.Id == 0) r.Id = store.Count + 1;
                store.Add(r);
                return Task.CompletedTask;
            });
        refunds
            .Setup(r => r.GetTrackedRefundAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => store.FirstOrDefault(r => r.Id == id));
        refunds
            .Setup(r => r.GetEnrollmentPaymentsForPaymentAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentPayment>());
        refunds
            .Setup(r => r.GetPendingEarningLinesForEnrollmentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeacherEarningLine>());
        refunds
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var finance = new Mock<ITeacherFinanceImpactService>();
        finance
            .Setup(f => f.IsAlreadyPaidForEnrollmentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var moyasar = new Mock<IPaymentGateway>();
        moyasar.SetupGet(g => g.ProviderName).Returns("Moyasar");
        moyasar.SetupGet(g => g.IsConfigured).Returns(true);
        moyasar
            .Setup(g => g.RefundAsync("pay_recorded", It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Qalam.Data.DTOs.Payment.GatewayRefundDto
            {
                Id = "ref_moyasar",
                Status = "refunded",
                AmountHalalas = 10000,
                Currency = "SAR"
            });

        var settings = new Mock<IPaymentGatewaySettingsProvider>();
        settings
            .Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Qalam.Data.DTOs.Platform.PaymentGatewaySettingsDto
            {
                ActiveProvider = "Moyasar"
            });

        var resolver = new PaymentGatewayResolver(
            new IPaymentGateway[] { new MockPaymentGateway(), moyasar.Object }, settings.Object);

        var service = new RefundService(
            refunds.Object, finance.Object, resolver,
            Mock.Of<IPaymentTransactionEventService>(), Mock.Of<IStudentWalletService>());

        return (service, moyasar, store);
    }

    [Fact]
    public async Task AdHocRefund_RequiresApproval_DoesNotMoveMoney()
    {
        var (service, gateway, _) = BuildService();

        var refund = await service.IssueRefundAsync(
            paymentId: 10, enrollmentId: 1, amount: 100m, currency: "SAR",
            reason: "test", initiatedByUserId: 5,
            destination: RefundDestination.OriginalMethod, requiresApproval: true);

        Assert.Equal(RefundStatus.RequiresApproval, refund.Status);
        gateway.Verify(
            g => g.RefundAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveRefund_SettlesThroughGateway()
    {
        var (service, gateway, _) = BuildService();

        var refund = await service.IssueRefundAsync(
            paymentId: 10, enrollmentId: 1, amount: 100m, currency: "SAR",
            reason: "test", initiatedByUserId: 5,
            destination: RefundDestination.OriginalMethod, requiresApproval: true);

        var approved = await service.ApproveRefundAsync(refund.Id, approvedByUserId: 7);

        Assert.Equal(RefundStatus.Succeeded, approved.Status);
        Assert.Equal("ref_moyasar", approved.ProviderRefundId);
        gateway.Verify(
            g => g.RefundAsync("pay_recorded", It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RejectRefund_MarksRejected_AndMovesNoMoney()
    {
        var (service, gateway, _) = BuildService();

        var refund = await service.IssueRefundAsync(
            paymentId: 10, enrollmentId: 1, amount: 100m, currency: "SAR",
            reason: "test", initiatedByUserId: 5,
            destination: RefundDestination.OriginalMethod, requiresApproval: true);

        var rejected = await service.RejectRefundAsync(refund.Id, rejectedByUserId: 7, reason: "duplicate");

        Assert.Equal(RefundStatus.Rejected, rejected.Status);
        gateway.Verify(
            g => g.RefundAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
