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

    public PaymentReconciliationSettings Reconciliation { get; set; } = new();

    public WalletSettings Wallet { get; set; } = new();

    public AppleIapSettings AppleIap { get; set; } = new();
}

public class WalletSettings
{
    /// <summary><see cref="Qalam.Data.Entity.Payment.Payment.PaymentProvider"/> for wallet-funded payments.</summary>
    public const string ProviderName = "Wallet";

    public decimal MinTopUp { get; set; } = 10m;

    public decimal MaxTopUp { get; set; } = 5000m;
}

public class PaymentReconciliationSettings
{
    /// <summary>When true, API hosts a background Moyasar reconciliation sweep.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Minutes between scheduled reconciliation runs (minimum 5).</summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>How far back to query Moyasar list APIs each run.</summary>
    public int LookbackDays { get; set; } = 7;

    /// <summary>Max pages to fetch per resource (payments / invoices) per run.</summary>
    public int MaxPagesPerRun { get; set; } = 5;
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
    /// Student app deep-link for same-tab web checkout return
    /// (e.g. https://student-staging.qalam.net.sa/payments/return).
    /// Used when the client does not send <c>appReturnUrl</c>, or as fallback on the API return page.
    /// </summary>
    public string AppReturnUrl { get; set; } = string.Empty;

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

public class AppleIapSettings
{
    public const string ProviderName = "AppleIap";

    /// <summary>App Store bundle ID (com.qalam.sa).</summary>
    public string BundleId { get; set; } = "com.qalam.sa";

    /// <summary>App Store Connect API issuer ID (UUID).</summary>
    public string IssuerId { get; set; } = string.Empty;

    /// <summary>App Store Connect API key ID.</summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Path to the .p8 private key file for App Store Server API.</summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>Environment: Production or Sandbox.</summary>
    public string Environment { get; set; } = "Sandbox";

    /// <summary>
    /// IAP price tiers in SAR. The backend maps amountDue to the nearest ceiling tier.
    /// </summary>
    public List<int> Tiers { get; set; } = new()
    {
        10, 15, 20, 25, 30, 40, 50, 60, 75, 100, 125, 150, 200, 250, 300, 400, 500
    };

    /// <summary>IAP product ID prefix: com.qalam.sa.session.tier_</summary>
    public string ProductIdPrefix { get; set; } = "com.qalam.sa.session.tier_";
}
