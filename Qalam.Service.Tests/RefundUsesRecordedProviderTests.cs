using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class RefundUsesRecordedProviderTests
{
    [Fact]
    public async Task IssueRefund_UsesPaymentProvider_NotActiveProvider()
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

        var refunds = new Mock<IRefundRepository>();
        refunds
            .Setup(r => r.GetTrackedPaymentWithRefundsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        refunds
            .Setup(r => r.AddRefundAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        refunds
            .Setup(r => r.GetEnrollmentPaymentsForPaymentAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentPayment>());
        refunds
            .Setup(r => r.GetPendingEarningLinesForEnrollmentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qalam.Data.Entity.Payment.TeacherEarningLine>());
        refunds
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var finance = new Mock<ITeacherFinanceImpactService>();
        finance
            .Setup(f => f.IsAlreadyPaidForEnrollmentAsync(1, It.IsAny<CancellationToken>()))
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

        var mockGw = new MockPaymentGateway();
        var settings = new Mock<IPaymentGatewaySettingsProvider>();
        settings
            .Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Qalam.Data.DTOs.Platform.PaymentGatewaySettingsDto
            {
                ActiveProvider = MockPaymentGateway.Name // active is Mock, payment is Moyasar
            });

        var resolver = new PaymentGatewayResolver(new IPaymentGateway[] { mockGw, moyasar.Object }, settings.Object);
        var service = new RefundService(refunds.Object, finance.Object, resolver, Mock.Of<IPaymentTransactionEventService>());

        var refund = await service.IssueRefundAsync(
            paymentId: 10,
            enrollmentId: 1,
            amount: 100m,
            currency: "SAR",
            reason: "test",
            initiatedByUserId: null);

        Assert.Equal("ref_moyasar", refund.ProviderRefundId);
        moyasar.Verify(
            g => g.RefundAsync("pay_recorded", It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
