using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AppleIapService : IAppleIapService
{
    private const string ClaimParticipantId = "pid";
    private const string ClaimProductId = "prd";
    private const string ClaimAmountSar = "amt";

    private readonly AppleIapSettings _settings;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<AppleIapService> _logger;

    public AppleIapService(
        IOptions<AppleIapSettings> settings,
        IHttpClientFactory http,
        ILogger<AppleIapService> logger)
    {
        _settings = settings.Value;
        _http = http;
        _logger = logger;
    }

    // ── product resolution ─────────────────────────────────────────────────

    public string? ResolveProductId(decimal amountSar) =>
        _settings.ResolveProductId(amountSar);

    // ── transaction token ──────────────────────────────────────────────────

    public string CreateTransactionToken(int participantId, int userId, string productId, decimal amountSar)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.TokenSigningSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimParticipantId, participantId.ToString()),
            new Claim(ClaimProductId, productId),
            new Claim(ClaimAmountSar, amountSar.ToString("F2"))
        };

        var token = new JwtSecurityToken(
            issuer: "qalam-iap",
            audience: "apple-iap",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TryParseTransactionToken(
        string token,
        out int participantId,
        out int userId,
        out string productId,
        out decimal amountSar)
    {
        participantId = 0;
        userId = 0;
        productId = string.Empty;
        amountSar = 0;

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.TokenSigningSecret));
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "qalam-iap",
                ValidateAudience = true,
                ValidAudience = "apple-iap",
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(2)
            }, out _);

            var pidClaim = principal.FindFirst(ClaimParticipantId)?.Value;
            var uidClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var prdClaim = principal.FindFirst(ClaimProductId)?.Value;
            var amtClaim = principal.FindFirst(ClaimAmountSar)?.Value;

            if (string.IsNullOrEmpty(pidClaim) ||
                string.IsNullOrEmpty(uidClaim) ||
                string.IsNullOrEmpty(prdClaim) ||
                string.IsNullOrEmpty(amtClaim))
                return false;

            if (!int.TryParse(pidClaim, out participantId) ||
                !int.TryParse(uidClaim, out userId) ||
                !decimal.TryParse(amtClaim, out amountSar))
                return false;

            productId = prdClaim;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IAP transactionToken validation failed");
            return false;
        }
    }

    // ── Apple App Store Server API verification ────────────────────────────

    public async Task<AppleTransactionInfo?> VerifyTransactionAsync(
        string transactionId, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
            return null; // disabled — caller falls through to mock/dev path

        if (string.IsNullOrEmpty(_settings.PrivateKeyPem) ||
            string.IsNullOrEmpty(_settings.IssuerId) ||
            string.IsNullOrEmpty(_settings.KeyId))
        {
            _logger.LogWarning("AppleIapSettings incomplete; skipping App Store Server API verification");
            return null;
        }

        try
        {
            var jwt = BuildAppleApiJwt();
            var baseUrl = _settings.UseSandbox
                ? "https://api.storekit-sandbox.itunes.apple.com"
                : "https://api.storekit.itunes.apple.com";
            var url = $"{baseUrl}/inApps/v1/transactions/{transactionId}";

            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync(url, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "App Store Server API returned {StatusCode} for transaction {TransactionId}: {Body}",
                    (int)response.StatusCode, transactionId, body);
                return null;
            }

            // Response is a JWS (signed JWT). Decode payload without signature verification
            // (transport security via HTTPS + TLS is sufficient for initial implementation).
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("signedTransactionInfo", out var jws))
            {
                _logger.LogWarning("App Store response missing signedTransactionInfo for {TransactionId}", transactionId);
                return null;
            }

            return DecodeJwsPayload(jws.GetString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Apple transaction {TransactionId}", transactionId);
            return null;
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private string BuildAppleApiJwt()
    {
        // Strip PEM header/footer and whitespace from the private key
        var pem = _settings.PrivateKeyPem
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("-----BEGIN EC PRIVATE KEY-----", "")
            .Replace("-----END EC PRIVATE KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        var keyBytes = Convert.FromBase64String(pem);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(keyBytes, out _);

        var key = new ECDsaSecurityKey(ecdsa) { KeyId = _settings.KeyId };
        var creds = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);

        var now = DateTimeOffset.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.IssuerId,
            Audience = "appstoreconnect-v1",
            IssuedAt = now.UtcDateTime,
            Expires = now.AddMinutes(5).UtcDateTime,
            SigningCredentials = creds,
            Claims = new Dictionary<string, object>
            {
                ["bid"] = _settings.BundleId
            }
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.CreateEncodedJwt(descriptor);
    }

    private static AppleTransactionInfo? DecodeJwsPayload(string jws)
    {
        if (string.IsNullOrEmpty(jws)) return null;

        var parts = jws.Split('.');
        if (parts.Length < 2) return null;

        // Pad the base64url-encoded payload
        var payload = parts[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new AppleTransactionInfo
            {
                BundleId = root.TryGetProperty("bundleId", out var b) ? b.GetString() ?? "" : "",
                ProductId = root.TryGetProperty("productId", out var p) ? p.GetString() ?? "" : "",
                ApplicationUsername = root.TryGetProperty("appAccountToken", out var a) ? a.GetString() ?? "" : "",
                Environment = root.TryGetProperty("environment", out var e) ? e.GetString() ?? "" : "",
                PurchaseDateMs = root.TryGetProperty("purchaseDate", out var d) ? d.GetInt64() : 0,
                TransactionId = root.TryGetProperty("transactionId", out var t) ? t.GetString() ?? "" : "",
            };
        }
        catch
        {
            return null;
        }
    }
}
