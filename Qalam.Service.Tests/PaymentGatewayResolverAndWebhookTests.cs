using Microsoft.Extensions.Options;
using Moq;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.DTOs.Platform;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Qalam.Service.Payments;

namespace Qalam.Service.Tests;

public class PaymentGatewayResolverTests
{
    private static PaymentGatewayResolver CreateResolver(
        string activeProvider,
        params IPaymentGateway[] gateways)
    {
        var settings = new Mock<IPaymentGatewaySettingsProvider>();
        settings
            .Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewaySettingsDto { ActiveProvider = activeProvider });
        return new PaymentGatewayResolver(gateways, settings.Object);
    }

    [Fact]
    public void Resolve_ByName_ReturnsGateway()
    {
        var mock = new MockPaymentGateway();
        var moyasar = CreateUnconfiguredMoyasar();
        var resolver = CreateResolver("Mock", mock, moyasar);

        Assert.Same(mock, resolver.Resolve("Mock"));
        Assert.Same(mock, resolver.Resolve("MOCK"));
        Assert.Same(moyasar, resolver.Resolve("Moyasar"));
    }

    [Fact]
    public void Resolve_Unknown_Throws()
    {
        var resolver = CreateResolver("Mock", new MockPaymentGateway());
        Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Nope"));
    }

    [Fact]
    public async Task ResolveActiveAsync_ReadsSettings()
    {
        var mock = new MockPaymentGateway();
        var moyasar = CreateUnconfiguredMoyasar();
        var resolver = CreateResolver("Mock", mock, moyasar);
        var active = await resolver.ResolveActiveAsync();
        Assert.Equal(MockPaymentGateway.Name, active.ProviderName);
    }

    [Fact]
    public async Task ResolveActiveAsync_UnconfiguredNonMock_Throws()
    {
        var mock = new MockPaymentGateway();
        var moyasar = CreateUnconfiguredMoyasar();
        var resolver = CreateResolver("Moyasar", mock, moyasar);
        await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveActiveAsync());
    }

    private static MoyasarPaymentGateway CreateUnconfiguredMoyasar()
    {
        var http = new HttpClient();
        return new MoyasarPaymentGateway(
            http,
            Options.Create(new PaymentSettings()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MoyasarPaymentGateway>.Instance);
    }
}

public class PaymentGatewayWebhookVerificationTests
{
    [Fact]
    public void Moyasar_Verify_RejectsBadSecret()
    {
        var settings = Options.Create(new PaymentSettings
        {
            Moyasar = new MoyasarPaymentSettings
            {
                WebhookSharedSecret = "correct-secret",
                PublishableApiKey = "pk",
                SecretApiKey = "sk"
            }
        });
        var gateway = new MoyasarPaymentGateway(
            new HttpClient(),
            settings,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MoyasarPaymentGateway>.Instance);

        var body = """{"secret_token":"wrong","type":"payment_paid","data":{"id":"pay_1"}}""";
        var result = gateway.VerifyAndParseWebhook(body, new Dictionary<string, string>());
        Assert.Equal(PaymentWebhookAuthResult.Unauthorized, result.Auth);
    }

    [Fact]
    public void Moyasar_Verify_AcceptsValidSecret()
    {
        var settings = Options.Create(new PaymentSettings
        {
            Moyasar = new MoyasarPaymentSettings
            {
                WebhookSharedSecret = "correct-secret",
                PublishableApiKey = "pk",
                SecretApiKey = "sk"
            }
        });
        var gateway = new MoyasarPaymentGateway(
            new HttpClient(),
            settings,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MoyasarPaymentGateway>.Instance);

        var body = """{"secret_token":"correct-secret","type":"payment_paid","data":{"id":"pay_1"}}""";
        var result = gateway.VerifyAndParseWebhook(body, new Dictionary<string, string>());
        Assert.Equal(PaymentWebhookAuthResult.Ok, result.Auth);
        Assert.Equal("pay_1", result.ProviderPaymentId);
    }

    [Fact]
    public void PayTabs_Hmac_IsDeterministic()
    {
        var fields = new Dictionary<string, string>
        {
            ["tran_ref"] = "TST1",
            ["cart_id"] = "cart1",
            ["response_status"] = "A",
            ["signature"] = "ignored"
        };
        var a = PayTabsPaymentGateway.ComputeHmacSignature(fields, "server-key");
        var b = PayTabsPaymentGateway.ComputeHmacSignature(fields, "server-key");
        Assert.Equal(a, b);
        Assert.NotEmpty(a);
    }

    [Fact]
    public void PayTabs_Verify_RejectsBadSignature()
    {
        var settings = Options.Create(new PaymentSettings
        {
            PayTabs = new PayTabsPaymentSettings
            {
                ProfileId = 1,
                ServerKey = "server-key"
            }
        });
        var gateway = new PayTabsPaymentGateway(
            new HttpClient(),
            settings,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PayTabsPaymentGateway>.Instance);

        var body = """{"tran_ref":"TST1","response_status":"A","signature":"deadbeef"}""";
        var result = gateway.VerifyAndParseWebhook(body, new Dictionary<string, string>());
        Assert.Equal(PaymentWebhookAuthResult.Unauthorized, result.Auth);
    }

    [Fact]
    public void Stripe_Signature_AcceptsValid()
    {
        var secret = "whsec_test";
        var body = """{"type":"payment_intent.succeeded","data":{"object":{"id":"pi_1","status":"succeeded"}}}""";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signed = $"{ts}.{body}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signed));
        var v1 = Convert.ToHexString(hash).ToLowerInvariant();
        var header = $"t={ts},v1={v1}";

        Assert.True(StripePaymentGateway.VerifyStripeSignature(body, header, secret, ts));

        var settings = Options.Create(new PaymentSettings
        {
            Stripe = new StripePaymentSettings
            {
                PublishableApiKey = "pk",
                SecretApiKey = "sk",
                WebhookSecret = secret
            }
        });
        var gateway = new StripePaymentGateway(
            new HttpClient(),
            settings,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<StripePaymentGateway>.Instance);
        var parsed = gateway.VerifyAndParseWebhook(body, new Dictionary<string, string>
        {
            ["Stripe-Signature"] = header
        });
        Assert.Equal(PaymentWebhookAuthResult.Ok, parsed.Auth);
        Assert.Equal("pi_1", parsed.ProviderPaymentId);
    }

    [Fact]
    public void HyperPay_AesGcm_RoundTrip()
    {
        var key = Convert.FromHexString(new string('a', 64));
        var iv = Convert.FromHexString(new string('b', 24));
        var plain = System.Text.Encoding.UTF8.GetBytes(
            """{"type":"PAYMENT","payload":{"id":"checkout_1","result":{"code":"000.000.000"}}}""");
        var ciphertext = HyperPayPaymentGateway.EncryptAesGcm(plain, key, iv, out var tag);

        var decrypted = HyperPayPaymentGateway.DecryptAesGcm(
            Convert.ToHexString(ciphertext),
            Convert.ToHexString(iv),
            Convert.ToHexString(tag),
            Convert.ToHexString(key));

        Assert.Contains("checkout_1", decrypted);

        var settings = Options.Create(new PaymentSettings
        {
            HyperPay = new HyperPayPaymentSettings
            {
                EntityId = "entity",
                AccessToken = "token",
                WebhookDecryptionKey = Convert.ToHexString(key)
            }
        });
        var gateway = new HyperPayPaymentGateway(
            new HttpClient(),
            settings,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<HyperPayPaymentGateway>.Instance);

        var result = gateway.VerifyAndParseWebhook(
            Convert.ToHexString(ciphertext),
            new Dictionary<string, string>
            {
                ["X-Initialization-Vector"] = Convert.ToHexString(iv),
                ["X-Authentication-Tag"] = Convert.ToHexString(tag)
            });
        Assert.Equal(PaymentWebhookAuthResult.Ok, result.Auth);
        Assert.Equal("checkout_1", result.ProviderPaymentId);
    }

    [Fact]
    public async Task MockGateway_Refund_ReturnsMockId()
    {
        var gateway = new MockPaymentGateway();
        var dto = await gateway.RefundAsync("MOCK-pay", 1500);
        Assert.StartsWith("MOCK-REF-", dto.Id);
        Assert.Equal(1500, dto.AmountHalalas);
    }

    [Fact]
    public void MinorUnitConverter_RoundTrips()
    {
        Assert.Equal(1050, MinorUnitConverter.ToHalalas(10.50m));
        Assert.Equal(10.50m, MinorUnitConverter.FromHalalas(1050));
    }
}
