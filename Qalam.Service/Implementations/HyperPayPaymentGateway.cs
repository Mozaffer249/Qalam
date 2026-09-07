using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;
using Qalam.Service.Payments;

namespace Qalam.Service.Implementations;

public class HyperPayPaymentGateway : IPaymentGateway
{
    public const string Name = "HyperPay";

    private readonly HttpClient _http;
    private readonly PaymentSettings _settings;
    private readonly ILogger<HyperPayPaymentGateway> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public HyperPayPaymentGateway(
        HttpClient http,
        IOptions<PaymentSettings> settings,
        ILogger<HyperPayPaymentGateway> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        var baseUrl = string.IsNullOrWhiteSpace(_settings.HyperPay.BaseUrl)
            ? "https://eu-prod.oppwa.com"
            : _settings.HyperPay.BaseUrl.TrimEnd('/');
        _http.BaseAddress = new Uri(baseUrl + "/");
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var token = _settings.HyperPay.AccessToken ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public string ProviderName => Name;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.HyperPay.EntityId)
        && !string.IsNullOrWhiteSpace(_settings.HyperPay.AccessToken);

    public PaymentClientMode ClientMode => PaymentClientMode.HostedRedirect;

    public async Task<GatewayCheckoutDto> CreateCheckoutAsync(
        GatewayCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("HyperPay is not configured.");

        var amountMajor = MinorUnitConverter.FromHalalas(request.AmountHalalas).ToString("0.00");
        var form = new Dictionary<string, string>
        {
            ["entityId"] = _settings.HyperPay.EntityId,
            ["amount"] = amountMajor,
            ["currency"] = request.Currency,
            ["paymentType"] = "DB",
            ["merchantTransactionId"] = request.GivenId
        };
        if (!string.IsNullOrWhiteSpace(_settings.HyperPay.ShopperResultUrl))
            form["shopperResultUrl"] = _settings.HyperPay.ShopperResultUrl;

        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync("v1/checkouts", content, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HyperPay checkout create failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            throw new InvalidOperationException($"HyperPay create failed ({(int)response.StatusCode}): {Truncate(raw)}");
        }

        var payload = JsonSerializer.Deserialize<HyperPayCheckoutResponse>(raw, JsonOptions)
            ?? throw new InvalidOperationException("HyperPay create returned empty body.");

        if (string.IsNullOrWhiteSpace(payload.Id))
            throw new InvalidOperationException("HyperPay create missing checkout id.");

        var baseHost = _http.BaseAddress?.GetLeftPart(UriPartial.Authority) ?? "https://eu-prod.oppwa.com";
        var redirectUrl =
            $"{baseHost}/v1/paymentWidgets.js?checkoutId={Uri.EscapeDataString(payload.Id)}";

        return new GatewayCheckoutDto
        {
            ClientMode = ClientMode,
            ProviderPaymentRef = payload.Id,
            RedirectUrl = redirectUrl,
            CallbackUrl = _settings.HyperPay.ShopperResultUrl
        };
    }

    public async Task<GatewayPaymentDto?> FetchAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId) || !IsConfigured)
            return null;

        var url = $"v1/checkouts/{Uri.EscapeDataString(providerPaymentId)}/payment?entityId={Uri.EscapeDataString(_settings.HyperPay.EntityId)}";
        using var response = await _http.GetAsync(url, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HyperPay fetch failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            return null;
        }

        var payload = JsonSerializer.Deserialize<HyperPayPaymentResponse>(raw, JsonOptions);
        return payload == null ? null : MapPayment(payload);
    }

    public async Task<GatewayRefundDto> RefundAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            ["entityId"] = _settings.HyperPay.EntityId,
            ["paymentType"] = "RF"
        };
        if (amountHalalas.HasValue)
        {
            form["amount"] = MinorUnitConverter.FromHalalas(amountHalalas.Value).ToString("0.00");
            form["currency"] = _settings.DefaultCurrency;
        }

        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync(
            $"v1/payments/{Uri.EscapeDataString(providerPaymentId)}",
            content,
            cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HyperPay refund failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            throw new InvalidOperationException($"HyperPay refund failed ({(int)response.StatusCode}): {Truncate(raw)}");
        }

        var payload = JsonSerializer.Deserialize<HyperPayPaymentResponse>(raw, JsonOptions)
            ?? throw new InvalidOperationException("HyperPay refund returned empty body.");

        return new GatewayRefundDto
        {
            Id = payload.Id ?? providerPaymentId,
            Status = payload.Result?.Code ?? "refunded",
            AmountHalalas = amountHalalas ?? MinorUnitConverter.ToHalalas(
                decimal.TryParse(payload.Amount, out var a) ? a : 0m),
            Currency = payload.Currency ?? _settings.DefaultCurrency,
            Message = payload.Result?.Description
        };
    }

    public Task<GatewayPaymentDto?> CaptureAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default)
        => FetchAsync(providerPaymentId, cancellationToken);

    public Task<GatewayPaymentDto?> VoidAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
        => FetchAsync(providerPaymentId, cancellationToken);

    public PaymentWebhookParseResult VerifyAndParseWebhook(
        string rawBody,
        IReadOnlyDictionary<string, string> headers)
    {
        var keyHex = _settings.HyperPay.WebhookDecryptionKey;
        if (string.IsNullOrWhiteSpace(keyHex))
            return PaymentWebhookParseResult.NotConfigured();

        if (string.IsNullOrWhiteSpace(rawBody))
            return PaymentWebhookParseResult.Ignored("empty");

        headers.TryGetValue("X-Initialization-Vector", out var ivHex);
        headers.TryGetValue("x-initialization-vector", out var ivHexAlt);
        ivHex ??= ivHexAlt;

        headers.TryGetValue("X-Authentication-Tag", out var tagHex);
        headers.TryGetValue("x-authentication-tag", out var tagHexAlt);
        tagHex ??= tagHexAlt;

        if (string.IsNullOrWhiteSpace(ivHex) || string.IsNullOrWhiteSpace(tagHex))
            return PaymentWebhookParseResult.Unauthorized();

        try
        {
            var ciphertextHex = ExtractCiphertext(rawBody);
            var plain = DecryptAesGcm(ciphertextHex, ivHex, tagHex, keyHex);
            using var doc = JsonDocument.Parse(plain);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            string? paymentId = null;
            PaymentStatus? mapped = null;

            if (root.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
            {
                if (payload.TryGetProperty("id", out var idEl))
                    paymentId = idEl.GetString();

                if (payload.TryGetProperty("result", out var result)
                    && result.TryGetProperty("code", out var codeEl))
                {
                    mapped = MapResultCode(codeEl.GetString());
                }
            }

            if (string.IsNullOrWhiteSpace(paymentId))
                return PaymentWebhookParseResult.Ignored("no payload.id");

            return PaymentWebhookParseResult.Success(paymentId, type, mapped);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HyperPay webhook decrypt/parse failed");
            return PaymentWebhookParseResult.Unauthorized();
        }
    }

    public static string DecryptAesGcm(string ciphertextHex, string ivHex, string tagHex, string keyHex)
    {
        var key = Convert.FromHexString(keyHex);
        var iv = Convert.FromHexString(ivHex);
        var tag = Convert.FromHexString(tagHex);
        var ciphertext = Convert.FromHexString(ciphertextHex);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(iv, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    public static byte[] EncryptAesGcm(byte[] plaintext, byte[] key, byte[] iv, out byte[] tag)
    {
        tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(iv, plaintext, ciphertext, tag);
        return ciphertext;
    }

    private static string ExtractCiphertext(string rawBody)
    {
        var trimmed = rawBody.Trim();
        if (trimmed.StartsWith('{'))
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.TryGetProperty("encryptedBody", out var enc))
                return enc.GetString() ?? trimmed;
            if (doc.RootElement.TryGetProperty("payload", out var payload)
                && payload.ValueKind == JsonValueKind.String)
                return payload.GetString() ?? trimmed;
        }

        return trimmed.Trim('"');
    }

    private static GatewayPaymentDto MapPayment(HyperPayPaymentResponse p)
    {
        var code = p.Result?.Code;
        var amountMajor = decimal.TryParse(p.Amount, out var amt) ? amt : 0m;
        return new GatewayPaymentDto
        {
            Id = p.Id ?? string.Empty,
            Status = code ?? string.Empty,
            MappedStatus = MapResultCode(code) ?? PaymentStatus.Pending,
            AmountHalalas = MinorUnitConverter.ToHalalas(amountMajor),
            Currency = p.Currency ?? "SAR",
            Message = p.Result?.Description
        };
    }

    private static PaymentStatus? MapResultCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        // Success: 000.000.* or 000.100.1*
        if (code.StartsWith("000.000.", StringComparison.Ordinal)
            || code.StartsWith("000.100.1", StringComparison.Ordinal))
            return PaymentStatus.Succeeded;

        if (code.StartsWith("000.200.", StringComparison.Ordinal)
            || code.StartsWith("800.400.5", StringComparison.Ordinal))
            return PaymentStatus.Pending;

        return PaymentStatus.Failed;
    }

    private static string Truncate(string? s, int max = 300)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);

    private sealed class HyperPayCheckoutResponse
    {
        public string? Id { get; set; }
    }

    private sealed class HyperPayPaymentResponse
    {
        public string? Id { get; set; }
        public string? Amount { get; set; }
        public string? Currency { get; set; }
        public HyperPayResult? Result { get; set; }
    }

    private sealed class HyperPayResult
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
    }
}
