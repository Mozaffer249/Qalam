using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.Payments.Queries.GetPaymentReceipt;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Shared;

namespace Qalam.Service.Tests;

public class PaymentIntentReuseTests
{
    [Fact]
    public async Task CreateAsync_NativeSdk_ReusesOpenPendingPayment()
    {
        var enrollment = BuildPendingEnrollment(enrollmentId: 10, amount: 100m);
        var existing = new Payment
        {
            Id = 55,
            PayerUserId = 7,
            Currency = "SAR",
            PaymentProvider = MoyasarPaymentGateway.Name,
            ProviderTransactionId = "existing-given-id",
            Subtotal = 100m,
            TotalAmount = 100m,
            Status = PaymentStatus.Pending,
            PaymentItems =
            {
                new PaymentItem
                {
                    ItemType = PaymentItemType.CourseEnrollment,
                    ReferenceId = 10,
                    Description = "Course",
                    Amount = 100m
                }
            }
        };

        var participantRepo = new Mock<IEnrollmentParticipantRepository>();
        participantRepo
            .Setup(r => r.GetByIdForPaymentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment.Participants.First());

        var paymentRepo = new Mock<IPaymentRepository>();
        paymentRepo
            .Setup(r => r.GetOpenIntentForEnrollmentAsync(10, MoyasarPaymentGateway.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var priceResolver = new Mock<IStudentCoursePriceResolver>();
        priceResolver.Setup(r => r.ResolveEnrollmentPayableAmount(It.IsAny<Enrollment>())).Returns(100m);

        var moyasar = new MoyasarPaymentGateway(
            new HttpClient(),
            Options.Create(new PaymentSettings
            {
                Moyasar = new MoyasarPaymentSettings
                {
                    PublishableApiKey = "pk_test",
                    SecretApiKey = "sk_test",
                    ApplePayMerchantId = "merchant.com.qalam",
                    ApplePayLabel = "Qalam"
                }
            }),
            NullLogger<MoyasarPaymentGateway>.Instance);

        var gatewayResolver = new Mock<IPaymentGatewayResolver>();
        gatewayResolver
            .Setup(r => r.ResolveActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(moyasar);

        var sut = new PaymentIntentService(
            participantRepo.Object,
            paymentRepo.Object,
            priceResolver.Object,
            gatewayResolver.Object,
            Options.Create(new PaymentSettings { DefaultCurrency = "SAR" }),
            NullLogger<PaymentIntentService>.Instance);

        var first = await sut.CreateAsync(1, 7);
        var second = await sut.CreateAsync(1, 7);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(55, first.Intent!.PaymentId);
        Assert.Equal(55, second.Intent!.PaymentId);
        Assert.Equal("existing-given-id", first.Intent.GivenId);
        Assert.Equal("existing-given-id", second.Intent.GivenId);
        Assert.Equal("merchant.com.qalam", first.Intent.ApplePayMerchantId);
        paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Hosted_CancelsPriorPendingAndCreatesNew()
    {
        var enrollment = BuildPendingEnrollment(enrollmentId: 22, amount: 50m);

        var participantRepo = new Mock<IEnrollmentParticipantRepository>();
        participantRepo
            .Setup(r => r.GetByIdForPaymentAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment.Participants.First());

        var paymentRepo = new Mock<IPaymentRepository>();
        paymentRepo
            .Setup(r => r.CancelOpenIntentsForEnrollmentAsync(22, "PayTabs", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        paymentRepo
            .Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) =>
            {
                p.Id = 99;
                return p;
            });

        var priceResolver = new Mock<IStudentCoursePriceResolver>();
        priceResolver.Setup(r => r.ResolveEnrollmentPayableAmount(It.IsAny<Enrollment>())).Returns(50m);

        var payTabs = new Mock<IPaymentGateway>();
        payTabs.SetupGet(g => g.ProviderName).Returns("PayTabs");
        payTabs.SetupGet(g => g.ClientMode).Returns(PaymentClientMode.HostedRedirect);
        payTabs.SetupGet(g => g.IsConfigured).Returns(true);
        payTabs
            .Setup(g => g.CreateCheckoutAsync(It.IsAny<GatewayCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayCheckoutDto
            {
                ClientMode = PaymentClientMode.HostedRedirect,
                ProviderPaymentRef = "tran-ref-1",
                RedirectUrl = "https://paytabs.example/pay",
                CallbackUrl = "https://api.example/Api/V1/Payments/Return/PayTabs"
            });

        var gatewayResolver = new Mock<IPaymentGatewayResolver>();
        gatewayResolver
            .Setup(r => r.ResolveActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(payTabs.Object);

        var sut = new PaymentIntentService(
            participantRepo.Object,
            paymentRepo.Object,
            priceResolver.Object,
            gatewayResolver.Object,
            Options.Create(new PaymentSettings { DefaultCurrency = "SAR" }),
            NullLogger<PaymentIntentService>.Instance);

        var result = await sut.CreateAsync(2, 7);

        Assert.True(result.Succeeded);
        Assert.Equal(99, result.Intent!.PaymentId);
        Assert.Equal("tran-ref-1", result.Intent.GivenId);
        paymentRepo.Verify(
            r => r.CancelOpenIntentsForEnrollmentAsync(22, "PayTabs", It.IsAny<CancellationToken>()),
            Times.Once);
        paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
    }

    private static Enrollment BuildPendingEnrollment(int enrollmentId, decimal amount)
    {
        var enrollment = new Enrollment
        {
            Id = enrollmentId,
            EnrollmentStatus = EnrollmentStatus.PendingPayment,
            OwnerUserId = 7,
            CourseId = 1,
            Course = new Course { Id = 1, Title = "Course", TeacherId = 3 },
            SelectedSessionSlots =
            {
                new EnrollmentSelectedSessionSlot
                {
                    SessionNumber = 1,
                    TeacherAvailabilityId = 1
                }
            }
        };
        var participant = new EnrollmentParticipant
        {
            Id = enrollmentId == 10 ? 1 : 2,
            EnrollmentId = enrollmentId,
            StudentId = 1,
            PaymentStatus = PaymentStatus.Pending,
            Enrollment = enrollment
        };
        enrollment.Participants.Add(participant);
        return enrollment;
    }
}

public class PaymentReceiptOwnershipTests
{
    [Fact]
    public async Task GetPaymentReceipt_RejectsOtherPayer()
    {
        var paymentRepo = new Mock<IPaymentRepository>();
        paymentRepo
            .Setup(r => r.GetReceiptAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment
            {
                Id = 5,
                PayerUserId = 42,
                Currency = "SAR",
                PaymentProvider = "Moyasar",
                TotalAmount = 10,
                Status = PaymentStatus.Succeeded
            });

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var handler = new GetPaymentReceiptQueryHandler(paymentRepo.Object, localizer.Object);
        var response = await handler.Handle(
            new GetPaymentReceiptQuery { PaymentId = 5, UserId = 7 },
            CancellationToken.None);

        Assert.False(response.Succeeded);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentReceipt_ReturnsOwnedPayment()
    {
        var paymentRepo = new Mock<IPaymentRepository>();
        paymentRepo
            .Setup(r => r.GetReceiptAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment
            {
                Id = 5,
                PayerUserId = 7,
                Currency = "SAR",
                PaymentProvider = "Moyasar",
                TotalAmount = 10,
                Subtotal = 10,
                Status = PaymentStatus.Succeeded,
                PaymentItems =
                {
                    new PaymentItem
                    {
                        ItemType = PaymentItemType.CourseEnrollment,
                        ReferenceId = 3,
                        Description = "Math",
                        Amount = 10
                    }
                }
            });

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var handler = new GetPaymentReceiptQueryHandler(paymentRepo.Object, localizer.Object);
        var response = await handler.Handle(
            new GetPaymentReceiptQuery { PaymentId = 5, UserId = 7 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.NotNull(response.Data);
        Assert.Equal(5, response.Data.PaymentId);
        Assert.Equal("Math", response.Data.Description);
    }
}
