using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Qalam.Core.Features.Student.Payments.Queries.GetPaymentReceipt;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PaymentIntentReuseTests
{
    private static Mock<IPaymentGatewaySettingsProvider> Settings(string moyasarMode = "NativeSdk")
    {
        var mock = new Mock<IPaymentGatewaySettingsProvider>();
        mock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewaySettingsDto
            {
                ActiveProvider = "Moyasar",
                MoyasarClientMode = moyasarMode
            });
        return mock;
    }

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
                    ClientMode = "NativeSdk",
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
            Settings("NativeSdk").Object,
            Mock.Of<IPaymentTransactionEventService>(),
            Options.Create(new PaymentSettings { DefaultCurrency = "SAR" }),
            NullLogger<PaymentIntentService>.Instance);

        var first = await sut.CreateAsync(1, 7);
        var second = await sut.CreateAsync(1, 7);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(55, first.Intent!.PaymentId);
        Assert.Equal(55, second.Intent!.PaymentId);
        Assert.Equal("existing-given-id", first.Intent.GivenId);
        Assert.Equal(PaymentClientMode.NativeSdk, first.Intent.ClientMode);
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
            Settings().Object,
            Mock.Of<IPaymentTransactionEventService>(),
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

    [Fact]
    public async Task CreateAsync_MoyasarHosted_UsesAdminClientMode_AndCancelsOpen()
    {
        var enrollment = BuildPendingEnrollment(enrollmentId: 33, amount: 75m);

        var participantRepo = new Mock<IEnrollmentParticipantRepository>();
        participantRepo
            .Setup(r => r.GetByIdForPaymentAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment.Participants.First());

        var paymentRepo = new Mock<IPaymentRepository>();
        paymentRepo
            .Setup(r => r.CancelOpenIntentsForEnrollmentAsync(33, MoyasarPaymentGateway.Name, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        paymentRepo
            .Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) =>
            {
                p.Id = 120;
                return p;
            });

        var priceResolver = new Mock<IStudentCoursePriceResolver>();
        priceResolver.Setup(r => r.ResolveEnrollmentPayableAmount(It.IsAny<Enrollment>())).Returns(75m);

        var moyasar = new Mock<IPaymentGateway>();
        moyasar.SetupGet(g => g.ProviderName).Returns(MoyasarPaymentGateway.Name);
        // Env/default gateway property still Native — admin override must win.
        moyasar.SetupGet(g => g.ClientMode).Returns(PaymentClientMode.NativeSdk);
        moyasar.SetupGet(g => g.IsConfigured).Returns(true);
        moyasar
            .Setup(g => g.CreateCheckoutAsync(It.IsAny<GatewayCheckoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GatewayCheckoutRequest req, CancellationToken _) =>
            {
                Assert.Equal(PaymentClientMode.HostedRedirect, req.PreferredClientMode);
                return new GatewayCheckoutDto
                {
                    ClientMode = PaymentClientMode.HostedRedirect,
                    ProviderPaymentRef = "inv_abc",
                    RedirectUrl = "https://checkout.moyasar.com/invoices/inv_abc",
                    CallbackUrl = "https://api.example/Api/V1/Payments/Return/Moyasar"
                };
            });

        var gatewayResolver = new Mock<IPaymentGatewayResolver>();
        gatewayResolver
            .Setup(r => r.ResolveActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(moyasar.Object);

        var sut = new PaymentIntentService(
            participantRepo.Object,
            paymentRepo.Object,
            priceResolver.Object,
            gatewayResolver.Object,
            Settings("HostedRedirect").Object,
            Mock.Of<IPaymentTransactionEventService>(),
            Options.Create(new PaymentSettings { DefaultCurrency = "SAR" }),
            NullLogger<PaymentIntentService>.Instance);

        var result = await sut.CreateAsync(3, 7);

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentClientMode.HostedRedirect, result.Intent!.ClientMode);
        Assert.Equal("inv_abc", result.Intent.GivenId);
        Assert.Equal("https://checkout.moyasar.com/invoices/inv_abc", result.Intent.RedirectUrl);
        paymentRepo.Verify(
            r => r.CancelOpenIntentsForEnrollmentAsync(33, MoyasarPaymentGateway.Name, It.IsAny<CancellationToken>()),
            Times.Once);
        paymentRepo.Verify(r => r.GetOpenIntentForEnrollmentAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
            Id = enrollmentId == 10 ? 1 : enrollmentId == 22 ? 2 : 3,
            EnrollmentId = enrollmentId,
            StudentId = 1,
            PaymentStatus = PaymentStatus.Pending,
            Enrollment = enrollment
        };
        enrollment.Participants.Add(participant);
        return enrollment;
    }
}

public class MoyasarHostedCheckoutTests
{
    [Fact]
    public async Task CreateCheckout_NativeSdk_ReturnsPublishableKey()
    {
        var gateway = new MoyasarPaymentGateway(
            new HttpClient(),
            Options.Create(new PaymentSettings
            {
                Moyasar = new MoyasarPaymentSettings
                {
                    PublishableApiKey = "pk_test",
                    SecretApiKey = "sk_test",
                    ClientMode = "NativeSdk",
                    CallbackUrl = "https://api.example/Api/V1/Payments/Return/Moyasar"
                }
            }),
            NullLogger<MoyasarPaymentGateway>.Instance);

        var dto = await gateway.CreateCheckoutAsync(new GatewayCheckoutRequest
        {
            GivenId = "g1",
            AmountHalalas = 1000,
            PreferredClientMode = PaymentClientMode.NativeSdk
        });

        Assert.Equal(PaymentClientMode.NativeSdk, dto.ClientMode);
        Assert.Equal("pk_test", dto.PublishableKey);
        Assert.True(string.IsNullOrEmpty(dto.RedirectUrl));
        Assert.Equal("g1", dto.ProviderPaymentRef);
    }

    [Fact]
    public async Task CreateCheckout_HostedRedirect_PostsInvoice()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Post
                    && m.RequestUri!.AbsolutePath.EndsWith("/invoices")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(
                    """{"id":"inv_1","status":"initiated","amount":1000,"currency":"SAR","url":"https://checkout.moyasar.com/invoices/inv_1"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var http = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.moyasar.com/v1/")
        };

        var gateway = new MoyasarPaymentGateway(
            http,
            Options.Create(new PaymentSettings
            {
                Moyasar = new MoyasarPaymentSettings
                {
                    PublishableApiKey = "pk_test",
                    SecretApiKey = "sk_test",
                    ClientMode = "HostedRedirect",
                    CallbackUrl = "https://api.example/Api/V1/Payments/Return/Moyasar"
                }
            }),
            NullLogger<MoyasarPaymentGateway>.Instance);

        var dto = await gateway.CreateCheckoutAsync(new GatewayCheckoutRequest
        {
            GivenId = "local-given",
            AmountHalalas = 1000,
            Currency = "SAR",
            Description = "Test",
            PreferredClientMode = PaymentClientMode.HostedRedirect
        });

        Assert.Equal(PaymentClientMode.HostedRedirect, dto.ClientMode);
        Assert.Equal("inv_1", dto.ProviderPaymentRef);
        Assert.Equal("https://checkout.moyasar.com/invoices/inv_1", dto.RedirectUrl);
        Assert.Contains("Payments/Return/Moyasar", dto.CallbackUrl);
    }

    [Fact]
    public void VerifyWebhook_InvoicePayload_ReturnsPaymentAndInvoiceIds()
    {
        var gateway = new MoyasarPaymentGateway(
            new HttpClient(),
            Options.Create(new PaymentSettings
            {
                Moyasar = new MoyasarPaymentSettings
                {
                    PublishableApiKey = "pk",
                    SecretApiKey = "sk",
                    WebhookSharedSecret = "secret"
                }
            }),
            NullLogger<MoyasarPaymentGateway>.Instance);

        var body = """
            {"id":"inv_9","status":"paid","url":"https://checkout.moyasar.com/invoices/inv_9","payments":[{"id":"pay_9","status":"paid","amount":1000}]}
            """;
        var parsed = gateway.VerifyAndParseWebhook(body, new Dictionary<string, string>());
        Assert.Equal(PaymentWebhookAuthResult.Ok, parsed.Auth);
        Assert.Equal("pay_9", parsed.ProviderPaymentId);
        Assert.Equal(PaymentStatus.Succeeded, parsed.MappedStatus);
        Assert.Contains("inv_9", parsed.AlternateProviderPaymentIds!);
    }

    [Fact]
    public void VerifyWebhook_PaymentPaid_IncludesInvoiceAlternate()
    {
        var gateway = new MoyasarPaymentGateway(
            new HttpClient(),
            Options.Create(new PaymentSettings
            {
                Moyasar = new MoyasarPaymentSettings
                {
                    PublishableApiKey = "pk",
                    SecretApiKey = "sk",
                    WebhookSharedSecret = "secret"
                }
            }),
            NullLogger<MoyasarPaymentGateway>.Instance);

        var body = """{"secret_token":"secret","type":"payment_paid","data":{"id":"pay_2","invoice_id":"inv_2"}}""";
        var parsed = gateway.VerifyAndParseWebhook(body, new Dictionary<string, string>());
        Assert.Equal(PaymentWebhookAuthResult.Ok, parsed.Auth);
        Assert.Equal("pay_2", parsed.ProviderPaymentId);
        Assert.Contains("inv_2", parsed.AlternateProviderPaymentIds!);
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
