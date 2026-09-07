namespace Qalam.Data.Helpers;

/// <summary>
/// Payment provider settings. Bound from environment (.env → PaymentSettings__*),
/// not from committed secrets. Active provider may be overridden at runtime via
/// SystemSettings key Payments.Gateway; API keys always stay here.
/// </summary>
public class PaymentSettings
{
    public const string SectionName = "PaymentSettings";

    /// <summary>Env default for active provider. Overridden by Payments.Gateway system setting when present.</summary>
    public string Provider { get; set; } = "Mock";

    public string MockProviderName { get; set; } = "MOCK";

    public string DefaultCurrency { get; set; } = "SAR";

    public MoyasarPaymentSettings Moyasar { get; set; } = new();

    public PayTabsPaymentSettings PayTabs { get; set; } = new();

    public HyperPayPaymentSettings HyperPay { get; set; } = new();

    public StripePaymentSettings Stripe { get; set; } = new();
}

public class MoyasarPaymentSettings
{
    public string BaseUrl { get; set; } = "https://api.moyasar.com/v1";

    /// <summary>Public key (pk_*) handed to the Flutter SDK. Safe to expose to clients.</summary>
    public string PublishableApiKey { get; set; } = string.Empty;

    /// <summary>Secret key (sk_*). Server-only — never return from any endpoint.</summary>
    public string SecretApiKey { get; set; } = string.Empty;

    /// <summary>Shared secret echoed by Moyasar webhooks as secret_token.</summary>
    public string WebhookSharedSecret { get; set; } = string.Empty;

    /// <summary>3DS / hosted return URL (browser redirect after payment).</summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// How the student app presents Moyasar checkout:
    /// <c>HostedRedirect</c> (default, WebView + invoices API) or <c>NativeSdk</c> (Flutter widgets).
    /// </summary>
    public string ClientMode { get; set; } = "HostedRedirect";

    /// <summary>Apple Pay merchant id registered in the Moyasar dashboard and Xcode (NativeSdk only).</summary>
    public string ApplePayMerchantId { get; set; } = string.Empty;

    /// <summary>Store name shown in the Apple Pay sheet (NativeSdk only).</summary>
    public string ApplePayLabel { get; set; } = "Qalam";

    public bool Use3ds { get; set; } = true;
}

public class PayTabsPaymentSettings
{
    public string BaseUrl { get; set; } = "https://secure.paytabs.sa";

    public int ProfileId { get; set; }

    /// <summary>Server key sent in the authorization header. Server-only.</summary>
    public string ServerKey { get; set; } = string.Empty;

    public string CallbackUrl { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;
}

public class HyperPayPaymentSettings
{
    public string BaseUrl { get; set; } = "https://eu-prod.oppwa.com";

    public string EntityId { get; set; } = string.Empty;

    /// <summary>Bearer access token. Server-only.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>64-char hex AES-GCM webhook decryption key.</summary>
    public string WebhookDecryptionKey { get; set; } = string.Empty;

    public string ShopperResultUrl { get; set; } = string.Empty;
}

public class StripePaymentSettings
{
    public string BaseUrl { get; set; } = "https://api.stripe.com";

    public string PublishableApiKey { get; set; } = string.Empty;

    /// <summary>Secret key (sk_*). Server-only.</summary>
    public string SecretApiKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public string SuccessUrl { get; set; } = string.Empty;

    public string CancelUrl { get; set; } = string.Empty;
}
