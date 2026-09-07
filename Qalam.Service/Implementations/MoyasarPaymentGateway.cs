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

public class MoyasarPaymentGateway : IPaymentGateway
{
    public const string Name = "Moyasar";

    private readonly HttpClient _http;
    private readonly PaymentSettings _settings;
    private readonly ILogger<MoyasarPaymentGateway> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MoyasarPaymentGateway(
        HttpClient http,
        IOptions<PaymentSettings> settings,
        ILogger<MoyasarPaymentGateway> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        var baseUrl = string.IsNullOrWhiteSpace(_settings.Moyasar.BaseUrl)
            ? "https://api.moyasar.com/v1"
            : _settings.Moyasar.BaseUrl.TrimEnd('/');
        _http.BaseAddress = new Uri(baseUrl + "/");
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var secret = _settings.Moyasar.SecretApiKey ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes(secret + ":"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
    }

    public string ProviderName => Name;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Moyasar.PublishableApiKey)
        && !string.IsNullOrWhiteSpace(_settings.Moyasar.SecretApiKey);

    public PaymentClientMode ClientMode => PaymentClientMode.NativeSdk;

    public Task<GatewayCheckoutDto> CreateCheckoutAsync(
        GatewayCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Moyasar is not configured.");

        return Task.FromResult(new GatewayCheckoutDto
        {
            ClientMode = ClientMode,
            ProviderPaymentRef = request.GivenId,
            PublishableKey = _settings.Moyasar.PublishableApiKey,
            CallbackUrl = string.IsNullOrWhiteSpace(request.CallbackUrl)
                ? _settings.Moyasar.CallbackUrl
                : request.CallbackUrl,
            ApplePayMerchantId = string.IsNullOrWhiteSpace(_settings.Moyasar.ApplePayMerchantId)
                ? null
                : _settings.Moyasar.ApplePayMerchantId.Trim(),
            ApplePayLabel = string.IsNullOrWhiteSpace(_settings.Moyasar.ApplePayLabel)
                ? "Qalam"
                : _settings.Moyasar.ApplePayLabel.Trim()
        });
    }

    public async Task<GatewayPaymentDto?> FetchAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return null;

        using var response = await _http.GetAsync($"payments/{providerPaymentId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Moyasar fetch failed: {Status} {Body}",
                (int)response.StatusCode,
                Truncate(body));
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<MoyasarPaymentResponse>(JsonOptions, cancellationToken);
        return payload == null ? null : MapPayment(payload);
    }

    public async Task<GatewayRefundDto> RefundAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default)
    {
        object body = amountHalalas.HasValue
            ? new { amount = amountHalalas.Value }
            : new { };

        using var response = await _http.PostAsJsonAsync(
            $"payments/{providerPaymentId}/refund",
            body,
            JsonOptions,
            cancellationToken);

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Moyasar refund failed for {PaymentId}: {Status} {Body}",
                providerPaymentId,
                (int)response.StatusCode,
                Truncate(raw));
            throw new InvalidOperationException(
                $"Moyasar refund failed ({(int)response.StatusCode}): {Truncate(raw)}");
        }

        var payload = JsonSerializer.Deserialize<MoyasarPaymentResponse>(raw, JsonOptions)
            ?? throw new InvalidOperationException("Moyasar refund returned empty body.");

        return new GatewayRefundDto
        {
            Id = payload.Id ?? providerPaymentId,
            Status = payload.Status ?? "refunded",
            AmountHalalas = payload.Amount,
            Currency = payload.Currency ?? "SAR",
            Message = payload.Source?.Message
        };
    }

    public async Task<GatewayPaymentDto?> CaptureAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default)
    {
        object body = amountHalalas.HasValue
            ? new { amount = amountHalalas.Value }
            : new { };

        using var response = await _http.PostAsJsonAsync(
            $"payments/{providerPaymentId}/capture",
            body,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Moyasar capture failed for {PaymentId}: {Status} {Body}",
                providerPaymentId,
                (int)response.StatusCode,
                Truncate(raw));
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<MoyasarPaymentResponse>(JsonOptions, cancellationToken);
        return payload == null ? null : MapPayment(payload);
    }

    public async Task<GatewayPaymentDto?> VoidAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync(
            $"payments/{providerPaymentId}/void",
            content: null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Moyasar void failed for {PaymentId}: {Status} {Body}",
                providerPaymentId,
                (int)response.StatusCode,
                Truncate(raw));
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<MoyasarPaymentResponse>(JsonOptions, cancellationToken);
        return payload == null ? null : MapPayment(payload);
    }

    public PaymentWebhookParseResult VerifyAndParseWebhook(
        string rawBody,
        IReadOnlyDictionary<string, string> headers)
    {
        var configuredSecret = _settings.Moyasar.WebhookSharedSecret;
        if (string.IsNullOrWhiteSpace(configuredSecret))
            return PaymentWebhookParseResult.NotConfigured();

        if (string.IsNullOrWhiteSpace(rawBody))
            return PaymentWebhookParseResult.Ignored("empty");

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            var secretToken = root.TryGetProperty("secret_token", out var st) ? st.GetString() : null;
            if (!ConstantTimeEquals(secretToken, configuredSecret))
                return PaymentWebhookParseResult.Unauthorized();

            var eventType = root.TryGetProperty("type", out var typeEl)
                ? typeEl.GetString()
                : (root.TryGetProperty("event", out var ev) ? ev.GetString() : null);

            string? paymentId = null;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                if (data.TryGetProperty("id", out var idEl))
                    paymentId = idEl.GetString();
            }

            if (string.IsNullOrWhiteSpace(paymentId))
                return PaymentWebhookParseResult.Ignored("no data.id");

            var mapped = eventType?.Trim().ToLowerInvariant() switch
            {
                "payment_paid" => PaymentStatus.Succeeded,
                "payment_failed" or "payment_faild" => PaymentStatus.Failed,
                "payment_refunded" => PaymentStatus.Refunded,
                _ => (PaymentStatus?)null
            };

            return PaymentWebhookParseResult.Success(paymentId, eventType, mapped);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Moyasar webhook parse failed");
            return PaymentWebhookParseResult.Ignored("parse_error");
        }
    }

    private static GatewayPaymentDto MapPayment(MoyasarPaymentResponse p) => new()
    {
        Id = p.Id ?? string.Empty,
        Status = p.Status ?? string.Empty,
        MappedStatus = MoyasarStatusMapper.Map(p.Status),
        AmountHalalas = p.Amount,
        Currency = p.Currency ?? "SAR",
        FeeHalalas = p.Fee,
        SourceCompany = p.Source?.Company,
        SourceNumber = p.Source?.Number,
        Message = p.Source?.Message,
        TransactionUrl = p.Source?.TransactionUrl
    };

    public static bool ConstantTimeEquals(string? a, string b)
    {
        if (a is null)
            return false;
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        if (aBytes.Length != bBytes.Length)
        {
            CryptographicOperations.FixedTimeEquals(aBytes, aBytes);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static string Truncate(string? s, int max = 300)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);

    private sealed class MoyasarPaymentResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public int Amount { get; set; }
        public int? Fee { get; set; }
        public string? Currency { get; set; }
        public MoyasarSourceResponse? Source { get; set; }
    }

    private sealed class MoyasarSourceResponse
    {
        public string? Type { get; set; }
        public string? Company { get; set; }
        public string? Number { get; set; }
        public string? Message { get; set; }
        [JsonPropertyName("transaction_url")]
        public string? TransactionUrl { get; set; }
    }
}
