namespace Qalam.Data.Helpers;

/// <summary>
/// Configuration for Apple In-App Purchase server-side verification.
/// Set via appsettings.json or environment variables (prefix: AppleIapSettings__).
/// </summary>
public class AppleIapSettings
{
    public const string SectionName = "AppleIapSettings";

    /// <summary>Set false to skip Apple verification (staging/dev mock flows).</summary>
    public bool Enabled { get; set; }

    /// <summary>App bundle ID as registered in App Store Connect (e.g. com.qalam.sa).</summary>
    public string BundleId { get; set; } = string.Empty;

    /// <summary>App Store Connect API Issuer ID (Users &amp; Access → Integrations → Keys).</summary>
    public string IssuerId { get; set; } = string.Empty;

    /// <summary>App Store Connect In-App Purchase key ID.</summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Content of the .p8 private key file (ES256 / ECDSA P-256).
    /// Paste the full PEM block including header/footer lines — never commit this value.
    /// </summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>Use Apple sandbox API endpoint (true for staging, false for production).</summary>
    public bool UseSandbox { get; set; }

    /// <summary>
    /// Secret used to sign/verify the transactionToken JWT (HMAC-SHA256 symmetric key).
    /// Must be at least 32 characters. Never commit this value.
    /// </summary>
    public string TokenSigningSecret { get; set; } = string.Empty;

    /// <summary>
    /// Prefix for IAP product IDs (e.g. "com.qalam.sa.session.tier_").
    /// Must match the product IDs registered in App Store Connect.
    /// </summary>
    public string ProductIdPrefix { get; set; } = "com.qalam.sa.session.tier_";

    /// <summary>
    /// Comma-separated SAR tier amounts that have products registered in App Store Connect
    /// (e.g. "10,15,20,25,30,40,50,60,75,100,125,150,200,250,300,400,500").
    /// </summary>
    public string TiersRaw { get; set; } = "10,15,20,25,30,40,50,60,75,100,125,150,200,250,300,400,500";

    private int[]? _tiers;

    public int[] Tiers => _tiers ??= TiersRaw
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(int.Parse)
        .OrderBy(x => x)
        .ToArray();

    /// <summary>Resolves the smallest tier product ID that covers <paramref name="amountSar"/>.</summary>
    public string? ResolveProductId(decimal amountSar)
    {
        var rounded = (int)Math.Ceiling(amountSar);
        if (rounded <= 0) return null;
        foreach (var tier in Tiers)
            if (tier >= rounded) return $"{ProductIdPrefix}{tier}";
        return null;
    }
}
