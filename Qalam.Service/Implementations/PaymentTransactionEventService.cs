using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PaymentTransactionEventService : IPaymentTransactionEventService
{
    private const int MaxPayloadLength = 8000;

    private static readonly HashSet<string> RedactedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "secret_token", "authorization", "signature", "stripe-signature",
        "number", "cvc", "cvv", "card_number", "pan",
        "email", "phone", "mobile", "name", "full_name",
        "token", "client_secret", "access_token", "password"
    };

    private readonly IPaymentTransactionEventRepository _events;
    private readonly ApplicationDBContext _db;
    private readonly ILogger<PaymentTransactionEventService> _logger;

    public PaymentTransactionEventService(
        IPaymentTransactionEventRepository events,
        ApplicationDBContext db,
        ILogger<PaymentTransactionEventService> logger)
    {
        _events = events;
        _db = db;
        _logger = logger;
    }

    public async Task<PaymentTransactionEvent> RecordAsync(
        PaymentTransactionEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var payloadHash = request.PayloadHash ?? ComputePayloadHash(request.RawPayload);
        var sanitized = string.IsNullOrWhiteSpace(request.RawPayload)
            ? null
            : SanitizePayload(request.RawPayload);

        var evt = new PaymentTransactionEvent
        {
            PaymentId = request.PaymentId,
            EnrollmentId = request.EnrollmentId,
            EnrollmentParticipantId = request.EnrollmentParticipantId,
            EnrollmentRequestId = request.EnrollmentRequestId,
            OpenSessionRequestId = request.OpenSessionRequestId,
            PaymentProvider = request.PaymentProvider,
            Source = request.Source,
            EventType = request.EventType,
            Result = request.Result,
            StatusBefore = request.StatusBefore,
            StatusAfter = request.StatusAfter,
            Amount = request.Amount,
            Currency = request.Currency,
            ProviderPaymentId = Truncate(request.ProviderPaymentId, 120),
            ProviderInvoiceId = Truncate(request.ProviderInvoiceId, 120),
            ProviderEventId = Truncate(request.ProviderEventId, 120),
            CorrelationId = Truncate(request.CorrelationId, 80),
            PayloadHash = payloadHash,
            PayloadJson = Truncate(sanitized, MaxPayloadLength),
            ErrorMessage = Truncate(request.ErrorMessage, 1000),
            Notes = Truncate(request.Notes, 500),
            ReceivedAt = request.ReceivedAt ?? now,
            ProcessedAt = request.ProcessedAt ?? now,
            CreatedAt = now
        };

        try
        {
            await _events.AddAsync(evt, cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit must not break payment flows.
            _logger.LogError(ex, "Failed to persist payment transaction event {EventType}", request.EventType);
        }

        return evt;
    }

    public async Task<bool> IsDuplicateDeliveryAsync(
        string paymentProvider,
        string? providerEventId,
        string? payloadHash,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(providerEventId)
            && await _events.ExistsByProviderEventIdAsync(paymentProvider, providerEventId, cancellationToken))
            return true;

        if (!string.IsNullOrWhiteSpace(payloadHash)
            && await _events.ExistsByPayloadHashAsync(paymentProvider, payloadHash, cancellationToken))
            return true;

        return false;
    }

    public async Task EnrichLinksFromPaymentAsync(
        PaymentTransactionEventRequest request,
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        request.PaymentId ??= payment.Id;
        request.Amount ??= payment.TotalAmount;
        request.Currency ??= payment.Currency;
        request.PaymentProvider = string.IsNullOrWhiteSpace(request.PaymentProvider)
            ? payment.PaymentProvider
            : request.PaymentProvider;
        request.ProviderPaymentId ??= payment.ProviderTransactionId;
        request.ProviderInvoiceId ??= payment.ProviderInvoiceId;

        if (request.EnrollmentId.HasValue
            && request.EnrollmentRequestId.HasValue
            && request.OpenSessionRequestId.HasValue)
            return;

        var enrollmentId = payment.PaymentItems?
            .FirstOrDefault(i => i.ItemType == PaymentItemType.CourseEnrollment)
            ?.ReferenceId;

        if (enrollmentId is null or <= 0)
            return;

        request.EnrollmentId ??= enrollmentId;

        var enrollment = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == enrollmentId.Value)
            .Select(e => new
            {
                e.EnrollmentRequestId,
                e.SessionRequestId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (enrollment == null)
            return;

        request.EnrollmentRequestId ??= enrollment.EnrollmentRequestId;
        request.OpenSessionRequestId ??= enrollment.SessionRequestId;
    }

    public Task<List<PaymentTransactionEvent>> ListForPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default) =>
        _events.ListForPaymentAsync(paymentId, cancellationToken);

    public string? ComputePayloadHash(string? rawBody) => ComputePayloadHashStatic(rawBody);

    public string? SanitizePayload(string? rawBody) => SanitizePayloadStatic(rawBody);

    public static string? ComputePayloadHashStatic(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawBody));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string? SanitizePayloadStatic(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            var node = JsonNode.Parse(rawBody);
            if (node != null)
                RedactNode(node);
            var json = node?.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            return Truncate(json, MaxPayloadLength);
        }
        catch
        {
            return Truncate(rawBody, Math.Min(2000, MaxPayloadLength));
        }
    }

    private static void RedactNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var prop in obj.ToList())
            {
                if (RedactedKeys.Contains(prop.Key))
                {
                    obj[prop.Key] = "[REDACTED]";
                    continue;
                }

                if (prop.Value != null)
                    RedactNode(prop.Value);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var child in arr)
            {
                if (child != null)
                    RedactNode(child);
            }
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return value.Length <= max ? value : value[..max];
    }
}
