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

    public PaymentClientMode ClientMode => ParseClientMode(_settings.Moyasar.ClientMode);

    public async Task<GatewayCheckoutDto> CreateCheckoutAsync(
        GatewayCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Moyasar is not configured.");

        var returnUrl = string.IsNullOrWhiteSpace(request.CallbackUrl)
            ? _settings.Moyasar.CallbackUrl
            : request.CallbackUrl;

        var mode = request.PreferredClientMode ?? ClientMode;

        if (mode == PaymentClientMode.NativeSdk)
        {
            return new GatewayCheckoutDto
            {
                ClientMode = PaymentClientMode.NativeSdk,
                ProviderPaymentRef = request.GivenId,
                PublishableKey = _settings.Moyasar.PublishableApiKey,
                CallbackUrl = returnUrl,
                ApplePayMerchantId = string.IsNullOrWhiteSpace(_settings.Moyasar.ApplePayMerchantId)
                    ? null
                    : _settings.Moyasar.ApplePayMerchantId.Trim(),
                ApplePayLabel = string.IsNullOrWhiteSpace(_settings.Moyasar.ApplePayLabel)
                    ? "Qalam"
                    : _settings.Moyasar.ApplePayLabel.Trim()
            };
        }

        // HostedRedirect: create a Moyasar invoice and return its checkout URL for WebView.
        if (string.IsNullOrWhiteSpace(returnUrl))
            throw new InvalidOperationException(
                "Moyasar hosted checkout requires PaymentSettings:Moyasar:CallbackUrl (success/back URL).");

        var body = new Dictionary<string, object?>
        {
            ["amount"] = request.AmountHalalas,
            ["currency"] = string.IsNullOrWhiteSpace(request.Currency) ? "SAR" : request.Currency,
            ["description"] = string.IsNullOrWhiteSpace(request.Description)
                ? "Qalam enrollment"
                : request.Description,
            ["success_url"] = returnUrl,
            ["back_url"] = returnUrl
        };

        if (request.Metadata.Count > 0)
            body["metadata"] = request.Metadata;

        // Optional invoice paid notification to our webhook (same route; parsed as invoice payload).
        var webhookUrl = BuildInvoiceNotifyUrl(returnUrl);
        if (!string.IsNullOrWhiteSpace(webhookUrl))
            body["callback_url"] = webhookUrl;

        using var response = await _http.PostAsJsonAsync("invoices", body, JsonOptions, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Moyasar create invoice failed: {Status} {Body}",
                (int)response.StatusCode,
                Truncate(raw));
            throw new InvalidOperationException(
                $"Moyasar create invoice failed ({(int)response.StatusCode}): {Truncate(raw)}");
        }

        var invoice = JsonSerializer.Deserialize<MoyasarInvoiceResponse>(raw, JsonOptions)
            ?? throw new InvalidOperationException("Moyasar invoice response was empty.");

        if (string.IsNullOrWhiteSpace(invoice.Id) || string.IsNullOrWhiteSpace(invoice.Url))
            throw new InvalidOperationException("Moyasar invoice response missing id or url.");

        return new GatewayCheckoutDto
        {
            ClientMode = PaymentClientMode.HostedRedirect,
            ProviderPaymentRef = invoice.Id,
            RedirectUrl = invoice.Url,
            CallbackUrl = returnUrl
        };
    }

    public async Task<GatewayPaymentDto?> FetchAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            return null;

        // Prefer payment fetch; hosted checkout stores invoice id until paid.
        var payment = await FetchPaymentAsync(providerPaymentId, cancellationToken);
        if (payment != null)
            return payment;

        return await FetchInvoiceAsPaymentAsync(providerPaymentId, cancellationToken);
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
        if (string.IsNullOrWhiteSpace(rawBody))
            return PaymentWebhookParseResult.Ignored("empty");

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            // Invoice paid callback (POST to invoice.callback_url) — no secret_token.
            if (LooksLikeInvoicePayload(root))
                return ParseInvoiceWebhook(root);

            var configuredSecret = _settings.Moyasar.WebhookSharedSecret;
            if (string.IsNullOrWhiteSpace(configuredSecret))
                return PaymentWebhookParseResult.NotConfigured();

            var secretToken = root.TryGetProperty("secret_token", out var st) ? st.GetString() : null;
            if (!ConstantTimeEquals(secretToken, configuredSecret))
                return PaymentWebhookParseResult.Unauthorized();

            var eventType = root.TryGetProperty("type", out var typeEl)
                ? typeEl.GetString()
                : (root.TryGetProperty("event", out var ev) ? ev.GetString() : null);

            string? paymentId = null;
            string? invoiceId = null;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                if (data.TryGetProperty("id", out var idEl))
                    paymentId = idEl.GetString();
                if (data.TryGetProperty("invoice_id", out var invEl))
                    invoiceId = invEl.GetString();
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

            IReadOnlyList<string>? alts = string.IsNullOrWhiteSpace(invoiceId)
                ? null
                : new[] { invoiceId };

            return PaymentWebhookParseResult.Success(paymentId, eventType, mapped, alternateProviderPaymentIds: alts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Moyasar webhook parse failed");
            return PaymentWebhookParseResult.Ignored("parse_error");
        }
    }

    private static bool LooksLikeInvoicePayload(JsonElement root)
    {
        if (!root.TryGetProperty("url", out _))
            return false;
        if (!root.TryGetProperty("status", out _))
            return false;
        // Classic webhook envelope has secret_token / type / data.
        if (root.TryGetProperty("secret_token", out _) || root.TryGetProperty("type", out _))
            return false;
        return root.TryGetProperty("id", out _);
    }

    private static PaymentWebhookParseResult ParseInvoiceWebhook(JsonElement root)
    {
        var invoiceId = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(invoiceId))
            return PaymentWebhookParseResult.Ignored("invoice_no_id");

        var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
        string? paymentId = null;
        if (root.TryGetProperty("payments", out var payments) && payments.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in payments.EnumerateArray())
            {
                var pStatus = p.TryGetProperty("status", out var ps) ? ps.GetString() : null;
                if (MoyasarStatusMapper.IsPaid(pStatus)
                    && p.TryGetProperty("id", out var pid))
                {
                    paymentId = pid.GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(paymentId))
            {
                foreach (var p in payments.EnumerateArray().Reverse())
                {
                    if (p.TryGetProperty("id", out var pid))
                    {
                        paymentId = pid.GetString();
                        break;
                    }
                }
            }
        }

        var mapped = status?.Trim().ToLowerInvariant() switch
        {
            "paid" => PaymentStatus.Succeeded,
            "failed" or "expired" or "canceled" or "voided" => PaymentStatus.Failed,
            "refunded" => PaymentStatus.Refunded,
            _ => MoyasarStatusMapper.IsPaid(status) ? PaymentStatus.Succeeded : (PaymentStatus?)null
        };

        // Prefer payment id for confirm/refund; keep invoice id as alternate for local lookup.
        var primary = !string.IsNullOrWhiteSpace(paymentId) ? paymentId! : invoiceId;
        var alts = !string.IsNullOrWhiteSpace(paymentId) && paymentId != invoiceId
            ? new[] { invoiceId }
            : null;

        return PaymentWebhookParseResult.Success(
            primary,
            eventType: "invoice_" + (status ?? "updated"),
            mappedStatus: mapped,
            alternateProviderPaymentIds: alts);
    }

    private async Task<GatewayPaymentDto?> FetchPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync($"payments/{paymentId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Moyasar fetch payment failed: {Status} {Body}",
                (int)response.StatusCode,
                Truncate(body));
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<MoyasarPaymentResponse>(JsonOptions, cancellationToken);
        return payload == null ? null : MapPayment(payload);
    }

    private async Task<GatewayPaymentDto?> FetchInvoiceAsPaymentAsync(
        string invoiceId,
        CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync($"invoices/{invoiceId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Moyasar fetch invoice failed: {Status} {Body}",
                (int)response.StatusCode,
                Truncate(body));
            return null;
        }

        var invoice = await response.Content.ReadFromJsonAsync<MoyasarInvoiceResponse>(JsonOptions, cancellationToken);
        if (invoice == null)
            return null;

        var paid = invoice.Payments?
            .FirstOrDefault(p => MoyasarStatusMapper.IsPaid(p.Status))
            ?? invoice.Payments?.LastOrDefault();

        if (paid != null && !string.IsNullOrWhiteSpace(paid.Id))
        {
            var mapped = MapPayment(paid);
            mapped.InvoiceId = invoice.Id;
            if (string.IsNullOrWhiteSpace(mapped.Status) && !string.IsNullOrWhiteSpace(invoice.Status))
            {
                mapped.Status = invoice.Status;
                mapped.MappedStatus = MapInvoiceStatus(invoice.Status);
            }

            return mapped;
        }

        // Invoice not yet paid — surface invoice status so confirm can fail cleanly.
        return new GatewayPaymentDto
        {
            Id = invoice.Id ?? invoiceId,
            Status = invoice.Status ?? "initiated",
            MappedStatus = MapInvoiceStatus(invoice.Status),
            AmountHalalas = invoice.Amount,
            Currency = invoice.Currency ?? "SAR",
            InvoiceId = invoice.Id
        };
    }

    private static PaymentStatus MapInvoiceStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return PaymentStatus.Pending;

        return status.Trim().ToLowerInvariant() switch
        {
            "paid" => PaymentStatus.Succeeded,
            "failed" or "expired" or "canceled" or "voided" => PaymentStatus.Failed,
            "refunded" => PaymentStatus.Refunded,
            _ => PaymentStatus.Pending
        };
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
        TransactionUrl = p.Source?.TransactionUrl,
        InvoiceId = p.InvoiceId
    };

    public static PaymentClientMode ParseClientMode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return PaymentClientMode.HostedRedirect;

        return raw.Trim().Equals("NativeSdk", StringComparison.OrdinalIgnoreCase)
            ? PaymentClientMode.NativeSdk
            : PaymentClientMode.HostedRedirect;
    }

    /// <summary>
    /// Derive webhook notify URL from the browser return URL when possible:
    /// .../Payments/Return/Moyasar → .../Payments/Webhooks/Moyasar
    /// </summary>
    private static string? BuildInvoiceNotifyUrl(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        try
        {
            var uri = new Uri(returnUrl);
            var path = uri.AbsolutePath;
            var replaced = path
                .Replace("/Payments/Return/", "/Payments/Webhooks/", StringComparison.OrdinalIgnoreCase);
            if (replaced.Equals(path, StringComparison.Ordinal))
                return null;

            return $"{uri.Scheme}://{uri.Authority}{replaced}";
        }
        catch
        {
            return null;
        }
    }

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

    private sealed class MoyasarInvoiceResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public int Amount { get; set; }
        public string? Currency { get; set; }
        public string? Url { get; set; }
        public List<MoyasarPaymentResponse>? Payments { get; set; }
    }

    private sealed class MoyasarPaymentResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public int Amount { get; set; }
        public int? Fee { get; set; }
        public string? Currency { get; set; }
        [JsonPropertyName("invoice_id")]
        public string? InvoiceId { get; set; }
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
