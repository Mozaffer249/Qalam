using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

/// <summary>
/// Validates Apple IAP transactions via the App Store Server API v2.
/// Uses a signed JWT (ES256) for authentication.
/// </summary>
public class AppleIapValidator : IAppleIapValidator
{
    private readonly AppleIapSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppleIapValidator> _logger;

    public AppleIapValidator(
        IOptions<PaymentSettings> paymentSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<AppleIapValidator> logger)
    {
        _settings = paymentSettings.Value.AppleIap;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AppleIapValidationResult> ValidateTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return AppleIapValidationResult.Fail("INVALID_INPUT", "transactionId is required.");

        try
        {
            var jwt = GenerateAppStoreJwt();
            var baseUrl = _settings.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? "https://api.storekit.itunes.apple.com"
                : "https://api.storekit-sandbox.itunes.apple.com";

            var client = _httpClientFactory.CreateClient("AppleIap");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var response = await client.GetAsync(
                $"{baseUrl}/inApps/v1/transactions/{transactionId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Apple IAP validation failed: {Status} {Body}", response.StatusCode, body);
                return AppleIapValidationResult.Fail("APPLE_API_ERROR",
                    $"App Store API returned {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            var signedTransaction = doc.RootElement.GetProperty("signedTransactionInfo").GetString();

            if (string.IsNullOrEmpty(signedTransaction))
                return AppleIapValidationResult.Fail("INVALID_RESPONSE", "Missing signedTransactionInfo.");

            var txPayload = DecodeJwsPayload(signedTransaction);
            var bundleId = txPayload.GetProperty("bundleId").GetString() ?? "";
            var productId = txPayload.GetProperty("productId").GetString() ?? "";
            var environment = txPayload.GetProperty("environment").GetString() ?? "";
            var priceMilliunits = txPayload.TryGetProperty("price", out var priceProp)
                ? priceProp.GetInt64() : 0;
            var currency = txPayload.TryGetProperty("currency", out var currProp)
                ? currProp.GetString() ?? "SAR" : "SAR";

            if (!bundleId.Equals(_settings.BundleId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Bundle ID mismatch: expected {Expected}, got {Actual}",
                    _settings.BundleId, bundleId);
                return AppleIapValidationResult.Fail("BUNDLE_MISMATCH",
                    "Transaction bundle ID does not match.");
            }

            return AppleIapValidationResult.Ok(bundleId, productId, environment, priceMilliunits, currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple IAP validation error for transaction {TransactionId}", transactionId);
            return AppleIapValidationResult.Fail("VALIDATION_ERROR", ex.Message);
        }
    }

    private string GenerateAppStoreJwt()
    {
        var keyPem = File.ReadAllText(_settings.PrivateKeyPath);
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(keyPem);

        var signingCredentials = new SigningCredentials(
            new ECDsaSecurityKey(ecdsa), SecurityAlgorithms.EcdsaSha256);

        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.IssuerId,
            IssuedAt = now,
            Expires = now.AddMinutes(20),
            Audience = "appstoreconnect-v1",
            SigningCredentials = signingCredentials,
        };
        descriptor.AdditionalHeaderClaims = new Dictionary<string, object>
        {
            ["kid"] = _settings.KeyId,
            ["bid"] = _settings.BundleId,
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.CreateEncodedJwt(descriptor);
    }

    private static JsonElement DecodeJwsPayload(string jws)
    {
        var parts = jws.Split('.');
        if (parts.Length < 2)
            throw new FormatException("Invalid JWS format.");

        var payload = parts[1];
        var remainder = payload.Length % 4;
        var padded = remainder switch
        {
            2 => payload + "==",
            3 => payload + "=",
            _ => payload,
        };
        padded = padded.Replace('-', '+');
        padded = padded.Replace('_', '/');
        var bytes = Convert.FromBase64String(padded);
        var json = Encoding.UTF8.GetString(bytes);
        return JsonDocument.Parse(json).RootElement;
    }
}
