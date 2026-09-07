using System.Net.Http.Headers;
using System.Net.Http.Json;
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

public class PayTabsPaymentGateway : IPaymentGateway
{
    public const string Name = "PayTabs";

    private readonly HttpClient _http;
    private readonly PaymentSettings _settings;
    private readonly ILogger<PayTabsPaymentGateway> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public PayTabsPaymentGateway(
        HttpClient http,
        IOptions<PaymentSettings> settings,
        ILogger<PayTabsPaymentGateway> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        var baseUrl = string.IsNullOrWhiteSpace(_settings.PayTabs.BaseUrl)
            ? "https://secure.paytabs.sa"
            : _settings.PayTabs.BaseUrl.TrimEnd('/');
        _http.BaseAddress = new Uri(baseUrl + "/");
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var serverKey = _settings.PayTabs.ServerKey ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(serverKey))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("authorization", serverKey);
    }

    public string ProviderName => Name;

    public bool IsConfigured =>
        _settings.PayTabs.ProfileId > 0
        && !string.IsNullOrWhiteSpace(_settings.PayTabs.ServerKey);

    public PaymentClientMode ClientMode => PaymentClientMode.HostedRedirect;

    public async Task<GatewayCheckoutDto> CreateCheckoutAsync(
        GatewayCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("PayTabs is not configured.");

        var body = new
        {
            profile_id = _settings.PayTabs.ProfileId,
            tran_type = "sale",
            tran_class = "ecom",
            cart_id = request.GivenId,
            cart_currency = request.Currency,
            cart_amount = MinorUnitConverter.FromHalalas(request.AmountHalalas),
            cart_description = request.Description,
            callback = string.IsNullOrWhiteSpace(request.CallbackUrl)
                ? _settings.PayTabs.CallbackUrl
                : request.CallbackUrl,
            @return = _settings.PayTabs.ReturnUrl
        };

        using var response = await _http.PostAsJsonAsync("payment/request", body, JsonOptions, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayTabs create failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            throw new InvalidOperationException($"PayTabs create failed ({(int)response.StatusCode}): {Truncate(raw)}");
        }

        var payload = JsonSerializer.Deserialize<PayTabsResponse>(raw, JsonOptions)
            ?? throw new InvalidOperationException("PayTabs create returned empty body.");

        if (string.IsNullOrWhiteSpace(payload.TranRef) || string.IsNullOrWhiteSpace(payload.RedirectUrl))
            throw new InvalidOperationException("PayTabs create missing tran_ref or redirect_url.");

        return new GatewayCheckoutDto
        {
            ClientMode = ClientMode,
            ProviderPaymentRef = payload.TranRef,
            RedirectUrl = payload.RedirectUrl,
            CallbackUrl = body.callback
        };
    }

    public async Task<GatewayPaymentDto?> FetchAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId) || !IsConfigured)
            return null;

        var body = new
        {
            profile_id = _settings.PayTabs.ProfileId,
            tran_ref = providerPaymentId
        };

        using var response = await _http.PostAsJsonAsync("payment/query", body, JsonOptions, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayTabs query failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            return null;
        }

        var payload = JsonSerializer.Deserialize<PayTabsResponse>(raw, JsonOptions);
        return payload == null ? null : MapPayment(payload);
    }

    public async Task<GatewayRefundDto> RefundAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["profile_id"] = _settings.PayTabs.ProfileId,
            ["tran_type"] = "refund",
            ["tran_class"] = "ecom",
            ["tran_ref"] = providerPaymentId,
            ["cart_id"] = providerPaymentId,
            ["cart_currency"] = _settings.DefaultCurrency,
            ["cart_description"] = "Refund"
        };
        if (amountHalalas.HasValue)
            body["cart_amount"] = MinorUnitConverter.FromHalalas(amountHalalas.Value);

        using var response = await _http.PostAsJsonAsync("payment/request", body, JsonOptions, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayTabs refund failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            throw new InvalidOperationException($"PayTabs refund failed ({(int)response.StatusCode}): {Truncate(raw)}");
        }

        var payload = JsonSerializer.Deserialize<PayTabsResponse>(raw, JsonOptions)
            ?? throw new InvalidOperationException("PayTabs refund returned empty body.");

        return new GatewayRefundDto
        {
            Id = payload.TranRef ?? providerPaymentId,
            Status = payload.PaymentResult?.ResponseStatus ?? "refunded",
            AmountHalalas = amountHalalas ?? 0,
            Currency = payload.CartCurrency ?? _settings.DefaultCurrency,
            Message = payload.PaymentResult?.ResponseMessage
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
        if (!IsConfigured)
            return PaymentWebhookParseResult.NotConfigured();

        if (string.IsNullOrWhiteSpace(rawBody))
            return PaymentWebhookParseResult.Ignored("empty");

        try
        {
            Dictionary<string, string> fields;
            if (rawBody.TrimStart().StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(rawBody);
                fields = FlattenJson(doc.RootElement);
            }
            else
            {
                fields = ParseForm(rawBody);
            }

            if (!fields.TryGetValue("signature", out var signature) || string.IsNullOrWhiteSpace(signature))
                return PaymentWebhookParseResult.Unauthorized();

            var expected = ComputeHmacSignature(fields, _settings.PayTabs.ServerKey);
            if (!ConstantTimeEquals(signature, expected))
                return PaymentWebhookParseResult.Unauthorized();

            fields.TryGetValue("tran_ref", out var tranRef);
            if (string.IsNullOrWhiteSpace(tranRef))
                return PaymentWebhookParseResult.Ignored("no tran_ref");

            fields.TryGetValue("payment_result.response_status", out var status);
            status ??= fields.GetValueOrDefault("response_status");

            var mapped = status?.Trim().ToUpperInvariant() switch
            {
                "A" => PaymentStatus.Succeeded,
                "D" or "E" => PaymentStatus.Failed,
                "C" => PaymentStatus.Cancelled,
                _ => (PaymentStatus?)null
            };

            return PaymentWebhookParseResult.Success(tranRef, status, mapped);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayTabs webhook parse failed");
            return PaymentWebhookParseResult.Ignored("parse_error");
        }
    }

    /// <summary>
    /// PayTabs signature: SHA-256 HMAC over sorted url-encoded non-empty params excluding signature.
    /// </summary>
    public static string ComputeHmacSignature(IDictionary<string, string> fields, string serverKey)
    {
        var pairs = fields
            .Where(kv => !kv.Key.Equals("signature", StringComparison.OrdinalIgnoreCase)
                         && !string.IsNullOrEmpty(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");

        var payload = string.Join("&", pairs);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(serverKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static GatewayPaymentDto MapPayment(PayTabsResponse p)
    {
        var status = p.PaymentResult?.ResponseStatus ?? string.Empty;
        var mapped = status.Trim().ToUpperInvariant() switch
        {
            "A" => PaymentStatus.Succeeded,
            "D" or "E" => PaymentStatus.Failed,
            "C" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };

        var amountMajor = decimal.TryParse(p.CartAmount, out var amt) ? amt : 0m;
        return new GatewayPaymentDto
        {
            Id = p.TranRef ?? string.Empty,
            Status = status,
            MappedStatus = mapped,
            AmountHalalas = MinorUnitConverter.ToHalalas(amountMajor),
            Currency = p.CartCurrency ?? "SAR",
            Message = p.PaymentResult?.ResponseMessage,
            TransactionUrl = p.RedirectUrl
        };
    }

    private static Dictionary<string, string> FlattenJson(JsonElement el, string prefix = "")
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (el.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in el.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var nested in FlattenJson(prop.Value, key))
                    result[nested.Key] = nested.Value;
            }
            else if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.ValueKind != JsonValueKind.Undefined)
            {
                result[key] = prop.Value.ToString();
            }
        }

        return result;
    }

    private static Dictionary<string, string> ParseForm(string raw)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            var key = Uri.UnescapeDataString(part[..idx]);
            var value = Uri.UnescapeDataString(part[(idx + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private static bool ConstantTimeEquals(string? a, string b)
        => MoyasarPaymentGateway.ConstantTimeEquals(a, b);

    private static string Truncate(string? s, int max = 300)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);

    private sealed class PayTabsResponse
    {
        [JsonPropertyName("tran_ref")]
        public string? TranRef { get; set; }

        [JsonPropertyName("redirect_url")]
        public string? RedirectUrl { get; set; }

        [JsonPropertyName("cart_amount")]
        public string? CartAmount { get; set; }

        [JsonPropertyName("cart_currency")]
        public string? CartCurrency { get; set; }

        [JsonPropertyName("payment_result")]
        public PayTabsPaymentResult? PaymentResult { get; set; }
    }

    private sealed class PayTabsPaymentResult
    {
        [JsonPropertyName("response_status")]
        public string? ResponseStatus { get; set; }

        [JsonPropertyName("response_message")]
        public string? ResponseMessage { get; set; }
    }
}
