using Microsoft.Extensions.Logging.Abstractions;
using Qalam.Data.DTOs.Payment;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PaymentTransactionEventSanitizeTests
{
    [Fact]
    public void SanitizePayload_RedactsSecretsAndCardFields()
    {
        var raw = """{"secret_token":"abc","data":{"id":"pay1","source":{"number":"4111111111111111","cvc":"123"},"email":"a@b.com"}}""";
        var sanitized = PaymentTransactionEventService.SanitizePayloadStatic(raw);
        Assert.NotNull(sanitized);
        Assert.DoesNotContain("abc", sanitized);
        Assert.DoesNotContain("411111", sanitized);
        Assert.Contains("[REDACTED]", sanitized);
        Assert.False(string.IsNullOrWhiteSpace(PaymentTransactionEventService.ComputePayloadHashStatic(raw)));
    }
}

public class MoyasarWebhookAliasAndInvoiceVerificationTests
{
    [Fact]
    public void InvoiceWebhook_RequiresRemoteVerification()
    {
        var gateway = new MoyasarPaymentGateway(
            new HttpClient(),
            Microsoft.Extensions.Options.Options.Create(new Qalam.Data.Helpers.PaymentSettings
            {
                Moyasar = new Qalam.Data.Helpers.MoyasarPaymentSettings
                {
                    PublishableApiKey = "pk",
                    SecretApiKey = "sk",
                    WebhookSharedSecret = "secret"
                }
            }),
            NullLogger<MoyasarPaymentGateway>.Instance);

        var body = """
            {"id":"inv_1","status":"paid","url":"https://checkout.moyasar.com/invoices/inv_1","payments":[{"id":"pay_1","status":"paid","amount":1000}]}
            """;
        var parsed = gateway.VerifyAndParseWebhook(body, new Dictionary<string, string>());
        Assert.Equal(PaymentWebhookAuthResult.Ok, parsed.Auth);
        Assert.True(parsed.RequiresRemoteVerification);
        Assert.Equal("pay_1", parsed.ProviderPaymentId);
    }

    [Fact]
    public void ClassicPaymentPaid_DoesNotRequireRemoteVerification()
    {
        var gateway = new MoyasarPaymentGateway(
            new HttpClient(),
            Microsoft.Extensions.Options.Options.Create(new Qalam.Data.Helpers.PaymentSettings
            {
                Moyasar = new Qalam.Data.Helpers.MoyasarPaymentSettings
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
        Assert.False(parsed.RequiresRemoteVerification);
    }
}

public class PaymentWebhookSingularRouteConstantTests
{
    [Fact]
    public void Router_DefinesSingularMoyasarWebhookAlias()
    {
        Assert.Equal("Api/V1/Payments/Webhook", Qalam.Data.AppMetaData.Router.MoyasarWebhookSingularAlias);
        Assert.Equal("Api/V1/Payments/Webhooks/{provider}", Qalam.Data.AppMetaData.Router.PaymentWebhook);
        Assert.Equal("Api/V1/Payments/Webhooks/Moyasar", Qalam.Data.AppMetaData.Router.MoyasarWebhook);
    }
}
