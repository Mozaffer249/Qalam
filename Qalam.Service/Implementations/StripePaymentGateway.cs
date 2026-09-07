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

public class StripePaymentGateway : IPaymentGateway
{
    public const string Name = "Stripe";

    private readonly HttpClient _http;
    private readonly PaymentSettings _settings;
    private readonly ILogger<StripePaymentGateway> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public StripePaymentGateway(
        HttpClient http,
        IOptions<PaymentSettings> settings,
        ILogger<StripePaymentGateway> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        var baseUrl = string.IsNullOrWhiteSpace(_settings.Stripe.BaseUrl)
            ? "https://api.stripe.com"
            : _settings.Stripe.BaseUrl.TrimEnd('/');
        _http.BaseAddress = new Uri(baseUrl + "/");
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var secret = _settings.Stripe.SecretApiKey ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(secret))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secret);
    }

    public string ProviderName => Name;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Stripe.SecretApiKey)
        && !string.IsNullOrWhiteSpace(_settings.Stripe.PublishableApiKey);

    public PaymentClientMode ClientMode => PaymentClientMode.HostedRedirect;

    public async Task<GatewayCheckoutDto> CreateCheckoutAsync(
        GatewayCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Stripe is not configured.");

        var form = new List<KeyValuePair<string, string>>
        {
            new("mode", "payment"),
            new("success_url", AppendSessionPlaceholder(_settings.Stripe.SuccessUrl)),
            new("cancel_url", _settings.Stripe.CancelUrl),
            new("client_reference_id", request.GivenId),
            new("line_items[0][price_data][currency]", request.Currency.ToLowerInvariant()),
            new("line_items[0][price_data][product_data][name]",
                string.IsNullOrWhiteSpace(request.Description) ? "Enrollment" : request.Description),
            new("line_items[0][price_data][unit_amount]", request.AmountHalalas.ToString()),
            new("line_items[0][quantity]", "1"),
            new("payment_intent_data[metadata][givenId]", request.GivenId)
        };

        foreach (var kv in request.Metadata)
            form.Add(new($"metadata[{kv.Key}]", kv.Value));

        using var content = new FormUrlEncodedContent(form);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/checkout/sessions")
        {
            Content = content
        };
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", request.GivenId);

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stripe checkout create failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            throw new InvalidOperationException($"Stripe create failed ({(int)response.StatusCode}): {Truncate(raw)}");
        }

        var payload = JsonSerializer.Deserialize<StripeCheckoutSession>(raw, JsonOptions)
            ?? throw new InvalidOperationException("Stripe create returned empty body.");

        if (string.IsNullOrWhiteSpace(payload.Id) || string.IsNullOrWhiteSpace(payload.Url))
            throw new InvalidOperationException("Stripe create missing id or url.");

        return new GatewayCheckoutDto
        {
            ClientMode = ClientMode,
            ProviderPaymentRef = payload.PaymentIntent ?? payload.Id,
            RedirectUrl = payload.Url,
            PublishableKey = _settings.Stripe.PublishableApiKey,
            CallbackUrl = _settings.Stripe.SuccessUrl
        };
    }

    public async Task<GatewayPaymentDto?> FetchAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return null;

        // Prefer payment_intents; fall back to checkout sessions.
        using var response = await _http.GetAsync(
            $"v1/payment_intents/{Uri.EscapeDataString(providerPaymentId)}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            using var sessionResponse = await _http.GetAsync(
                $"v1/checkout/sessions/{Uri.EscapeDataString(providerPaymentId)}",
                cancellationToken);
            if (!sessionResponse.IsSuccessStatusCode)
                return null;

            var sessionRaw = await sessionResponse.Content.ReadAsStringAsync(cancellationToken);
            var session = JsonSerializer.Deserialize<StripeCheckoutSession>(sessionRaw, JsonOptions);
            if (session?.PaymentIntent == null)
                return null;

            return await FetchAsync(session.PaymentIntent, cancellationToken);
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stripe fetch failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            return null;
        }

        var payload = JsonSerializer.Deserialize<StripePaymentIntent>(raw, JsonOptions);
        return payload == null ? null : MapPayment(payload);
    }

    public async Task<GatewayRefundDto> RefundAsync(
        string providerPaymentId,
        int? amountHalalas = null,
        CancellationToken cancellationToken = default)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("payment_intent", providerPaymentId)
        };
        if (amountHalalas.HasValue)
            form.Add(new("amount", amountHalalas.Value.ToString()));

        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync("v1/refunds", content, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stripe refund failed: {Status} {Body}", (int)response.StatusCode, Truncate(raw));
            throw new InvalidOperationException($"Stripe refund failed ({(int)response.StatusCode}): {Truncate(raw)}");
        }

        var payload = JsonSerializer.Deserialize<StripeRefund>(raw, JsonOptions)
            ?? throw new InvalidOperationException("Stripe refund returned empty body.");

        return new GatewayRefundDto
        {
            Id = payload.Id ?? providerPaymentId,
            Status = payload.Status ?? "succeeded",
            AmountHalalas = payload.Amount,
            Currency = payload.Currency?.ToUpperInvariant() ?? _settings.DefaultCurrency
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
        var secret = _settings.Stripe.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return PaymentWebhookParseResult.NotConfigured();

        headers.TryGetValue("Stripe-Signature", out var signatureHeader);
        headers.TryGetValue("stripe-signature", out var signatureHeaderAlt);
        signatureHeader ??= signatureHeaderAlt;

        if (string.IsNullOrWhiteSpace(signatureHeader)
            || !VerifyStripeSignature(rawBody, signatureHeader, secret))
            return PaymentWebhookParseResult.Unauthorized();

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var eventType = root.TryGetProperty("type", out var t) ? t.GetString() : null;

            string? paymentId = null;
            PaymentStatus? mapped = null;

            if (root.TryGetProperty("data", out var data)
                && data.TryGetProperty("object", out var obj))
            {
                if (obj.TryGetProperty("id", out var idEl))
                    paymentId = idEl.GetString();

                // Checkout session completed → payment_intent
                if (obj.TryGetProperty("payment_intent", out var pi)
                    && pi.ValueKind == JsonValueKind.String)
                    paymentId = pi.GetString();

                if (obj.TryGetProperty("status", out var statusEl))
                    mapped = MapStatus(statusEl.GetString());
            }

            mapped ??= eventType switch
            {
                "payment_intent.succeeded" or "checkout.session.completed" => PaymentStatus.Succeeded,
                "payment_intent.payment_failed" => PaymentStatus.Failed,
                "charge.refunded" or "payment_intent.canceled" => PaymentStatus.Refunded,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(paymentId))
                return PaymentWebhookParseResult.Ignored("no payment id");

            return PaymentWebhookParseResult.Success(paymentId, eventType, mapped);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook parse failed");
            return PaymentWebhookParseResult.Ignored("parse_error");
        }
    }

    public static bool VerifyStripeSignature(
        string rawBody,
        string signatureHeader,
        string webhookSecret,
        long? nowUnix = null,
        int toleranceSeconds = 300)
    {
        string? timestamp = null;
        string? v1 = null;
        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0] == "t") timestamp = kv[1];
            if (kv[0] == "v1") v1 = kv[1];
        }

        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(v1))
            return false;

        if (!long.TryParse(timestamp, out var ts))
            return false;

        var now = nowUnix ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - ts) > toleranceSeconds)
            return false;

        var signedPayload = $"{timestamp}.{rawBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return MoyasarPaymentGateway.ConstantTimeEquals(v1.ToLowerInvariant(), expected);
    }

    private static string AppendSessionPlaceholder(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "https://localhost/payments/return?session_id={CHECKOUT_SESSION_ID}";

        return url.Contains("{CHECKOUT_SESSION_ID}", StringComparison.Ordinal)
            ? url
            : url + (url.Contains('?') ? "&" : "?") + "session_id={CHECKOUT_SESSION_ID}";
    }

    private static GatewayPaymentDto MapPayment(StripePaymentIntent p)
    {
        return new GatewayPaymentDto
        {
            Id = p.Id ?? string.Empty,
            Status = p.Status ?? string.Empty,
            MappedStatus = MapStatus(p.Status) ?? PaymentStatus.Pending,
            AmountHalalas = p.Amount,
            Currency = p.Currency?.ToUpperInvariant() ?? "SAR",
            FeeHalalas = null,
            Message = p.LastPaymentError?.Message
        };
    }

    private static PaymentStatus? MapStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "succeeded" or "complete" => PaymentStatus.Succeeded,
        "canceled" => PaymentStatus.Cancelled,
        "requires_payment_method" or "requires_action" or "processing" or "open" => PaymentStatus.Pending,
        "failed" => PaymentStatus.Failed,
        _ => null
    };

    private static string Truncate(string? s, int max = 300)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);

    private sealed class StripeCheckoutSession
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        [JsonPropertyName("payment_intent")]
        public string? PaymentIntent { get; set; }
    }

    private sealed class StripePaymentIntent
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public int Amount { get; set; }
        public string? Currency { get; set; }
        [JsonPropertyName("last_payment_error")]
        public StripeError? LastPaymentError { get; set; }
    }

    private sealed class StripeError
    {
        public string? Message { get; set; }
    }

    private sealed class StripeRefund
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public int Amount { get; set; }
        public string? Currency { get; set; }
    }
}
