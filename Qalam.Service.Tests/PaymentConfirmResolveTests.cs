using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Core.Features.Student.Payments.Commands.ConfirmPayment;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PaymentConfirmResolveTests
{
    [Fact]
    public async Task Resolve_PaymentId_FindsLocalRow_ByInvoiceId()
    {
        // Client sends Moyasar payment id; DB still stores invoice id.
        var payment = new Payment
        {
            Id = 10,
            PayerUserId = 7,
            ProviderTransactionId = "inv_abc",
            PaymentProvider = "Moyasar",
            Status = PaymentStatus.Pending,
            TotalAmount = 10m,
            Currency = "SAR"
        };

        var paymentRepo = new Mock<IPaymentRepository>();
        paymentRepo
            .Setup(r => r.GetByProviderTransactionIdAsync("pay_xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        paymentRepo
            .Setup(r => r.GetByProviderTransactionIdAsync("inv_abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(g => g.ProviderName).Returns("Moyasar");
        gateway
            .Setup(g => g.FetchAsync("pay_xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentDto
            {
                Id = "pay_xyz",
                Status = "paid",
                MappedStatus = PaymentStatus.Succeeded,
                AmountHalalas = 1000,
                Currency = "SAR",
                InvoiceId = "inv_abc"
            });

        var resolver = new Mock<IPaymentGatewayResolver>();
        resolver.SetupGet(r => r.All).Returns(new[] { gateway.Object });

        var sut = CreateSut(paymentRepo.Object, resolver.Object);

        var ownership = await sut.ResolveLocalPaymentAsync("pay_xyz");
        Assert.NotNull(ownership);
        Assert.Equal(10, ownership!.PaymentId);
        Assert.Equal(7, ownership.PayerUserId);
    }

    [Fact]
    public async Task Resolve_InvoiceId_FindsLocalRow_AfterPromoteToPaymentId()
    {
        // Client still has invoice givenId; webhook already promoted ProviderTransactionId to payment id.
        var payment = new Payment
        {
            Id = 11,
            PayerUserId = 7,
            ProviderTransactionId = "pay_xyz",
            PaymentProvider = "Moyasar",
            Status = PaymentStatus.Succeeded,
            TotalAmount = 10m,
            Currency = "SAR"
        };

        var paymentRepo = new Mock<IPaymentRepository>();
        paymentRepo
            .Setup(r => r.GetByProviderTransactionIdAsync("inv_abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        paymentRepo
            .Setup(r => r.GetByProviderTransactionIdAsync("pay_xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(g => g.ProviderName).Returns("Moyasar");
        // Fetch(invoice) maps nested paid payment → Id = payment id, InvoiceId = invoice.
        gateway
            .Setup(g => g.FetchAsync("inv_abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentDto
            {
                Id = "pay_xyz",
                Status = "paid",
                MappedStatus = PaymentStatus.Succeeded,
                AmountHalalas = 1000,
                Currency = "SAR",
                InvoiceId = "inv_abc"
            });

        var resolver = new Mock<IPaymentGatewayResolver>();
        resolver.SetupGet(r => r.All).Returns(new[] { gateway.Object });

        var sut = CreateSut(paymentRepo.Object, resolver.Object);

        var ownership = await sut.ResolveLocalPaymentAsync("inv_abc");
        Assert.NotNull(ownership);
        Assert.Equal(11, ownership!.PaymentId);
    }

    [Fact]
    public async Task ConfirmHandler_UsesResolve_NotStrictLookup_ThenConfirms()
    {
        var confirmation = new Mock<IPaymentConfirmationService>();
        confirmation
            .Setup(c => c.ResolveLocalPaymentAsync("pay_xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOwnershipDto { PaymentId = 10, PayerUserId = 7 });
        confirmation
            .Setup(c => c.ConfirmFromGatewayAsync("pay_xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentConfirmationOutcome.Ok(new PaymentResultDto
            {
                PaymentId = 10,
                Status = PaymentStatus.Succeeded,
                TotalAmount = 10,
                Currency = "SAR",
                EnrollmentActivated = true
            }));

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var handler = new ConfirmPaymentCommandHandler(confirmation.Object, localizer.Object);
        var response = await handler.Handle(
            new ConfirmPaymentCommand
            {
                UserId = 7,
                Data = new ConfirmPaymentRequestDto { GivenId = "pay_xyz" }
            },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(10, response.Data!.PaymentId);
        confirmation.Verify(
            c => c.ResolveLocalPaymentAsync("pay_xyz", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmHandler_Returns404_WhenResolveMisses()
    {
        var confirmation = new Mock<IPaymentConfirmationService>();
        confirmation
            .Setup(c => c.ResolveLocalPaymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentOwnershipDto?)null);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var handler = new ConfirmPaymentCommandHandler(confirmation.Object, localizer.Object);
        var response = await handler.Handle(
            new ConfirmPaymentCommand
            {
                UserId = 7,
                Data = new ConfirmPaymentRequestDto { GivenId = "unknown" }
            },
            CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        confirmation.Verify(
            c => c.ConfirmFromGatewayAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmHandler_RejectsNonPayer()
    {
        var confirmation = new Mock<IPaymentConfirmationService>();
        confirmation
            .Setup(c => c.ResolveLocalPaymentAsync("inv_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOwnershipDto { PaymentId = 10, PayerUserId = 7 });

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var handler = new ConfirmPaymentCommandHandler(confirmation.Object, localizer.Object);
        var response = await handler.Handle(
            new ConfirmPaymentCommand
            {
                UserId = 99,
                Data = new ConfirmPaymentRequestDto { GivenId = "inv_1" }
            },
            CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        confirmation.Verify(
            c => c.ConfirmFromGatewayAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static PaymentConfirmationService CreateSut(
        IPaymentRepository paymentRepo,
        IPaymentGatewayResolver gatewayResolver)
    {
        return new PaymentConfirmationService(
            paymentRepo,
            Mock.Of<IEnrollmentRepository>(),
            Mock.Of<IEnrollmentPaymentRepository>(),
            Mock.Of<ITeacherAvailabilityRepository>(),
            Mock.Of<ICourseScheduleRepository>(),
            Mock.Of<IScheduleGenerationService>(),
            Mock.Of<IOpenSessionRequestReleaseService>(),
            Mock.Of<IRefundService>(),
            gatewayResolver,
            NullLogger<PaymentConfirmationService>.Instance);
    }
}
