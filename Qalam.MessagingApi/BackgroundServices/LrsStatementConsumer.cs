using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Data;
using Qalam.MessagingApi.Models;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.BackgroundServices;

public class LrsStatementConsumer : RabbitMqConsumerBase
{
    public const string LivenessName = "Lrs";

    private const string RetryCountHeader = "x-retry-count";
    private const string ErrorHeader = "x-error";
    private const string FailedAtHeader = "x-failed-at";

    private readonly ILogger<LrsStatementConsumer> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private IChannel? _channel;

    public LrsStatementConsumer(
        ILogger<LrsStatementConsumer> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IConsumerLivenessTracker liveness)
        : base(rabbitSettings, liveness, logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
    }

    protected override string ConsumerName => LivenessName;

    protected override async Task SetupAndConsumeAsync(IChannel channel, CancellationToken stoppingToken)
    {
        var enabled = await IsEnabledInDbAsync();
        if (!enabled)
        {
            _logger.LogInformation("LRS consumer disabled (not enabled in DB settings). Idling.");
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        _channel = channel;

        await channel.QueueDeclareAsync(
            queue: Settings.LrsQueueName,
            durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: Settings.LrsDeadLetterQueueName,
            durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var retryCount = GetRetryCount(ea.BasicProperties);
            LrsStatementMessage? message = null;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                message = JsonSerializer.Deserialize<LrsStatementMessage>(body);

                if (message == null || string.IsNullOrEmpty(message.StatementJson))
                {
                    _logger.LogWarning("Discarding null/invalid LRS payload to DLQ");
                    await PublishToDlqAsync(ea.Body.ToArray(), ea.BasicProperties, "Invalid or null LRS payload");
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var result = await SendToLrsAsync(message.StatementJson, stoppingToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "LRS statement sent ({StatusCode}): Verb={Verb}, Actor={Actor}, Object={Object}, Response={Response}",
                        result.HttpStatusCode, message.Verb, message.ActorName, message.ObjectId, result.ErrorMessage);
                    await UpdateLogAsync(message.LogId, LrsStatementStatus.Sent, (int?)result.HttpStatusCode, null);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else if (result.HttpStatusCode.HasValue && result.HttpStatusCode.Value >= 400 && result.HttpStatusCode.Value < 500)
                {
                    _logger.LogWarning(
                        "LRS permanent failure ({StatusCode}), moving to DLQ: {Error}",
                        result.HttpStatusCode, result.ErrorMessage);
                    await UpdateLogAsync(message.LogId, LrsStatementStatus.DeadLettered, (int?)result.HttpStatusCode, result.ErrorMessage);
                    await PublishToDlqAsync(ea.Body.ToArray(), ea.BasicProperties,
                        $"HTTP {result.HttpStatusCode}: {result.ErrorMessage}");
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else
                {
                    throw new HttpRequestException(
                        $"LRS transient failure (HTTP {result.HttpStatusCode}): {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LRS consumer error (Verb={Verb}, Retry={Retry})",
                    message?.Verb, retryCount);

                var maxRetries = Math.Max(0, await LoadMaxRetriesAsync());
                var payload = ea.Body.ToArray();

                if (retryCount >= maxRetries)
                {
                    _logger.LogWarning(
                        "LRS max retries reached ({Retry}/{Max}), moving to DLQ",
                        retryCount, maxRetries);
                    await UpdateLogAsync(message?.LogId, LrsStatementStatus.DeadLettered, null, ex.Message);
                    await PublishToDlqAsync(payload, ea.BasicProperties, ex.Message);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else
                {
                    await UpdateLogAsync(message?.LogId, LrsStatementStatus.Failed, null, ex.Message, retryCount + 1);
                    await RepublishForRetryAsync(payload, retryCount + 1);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: Settings.LrsQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "LrsStatementConsumer listening on queue: {Queue} (DLQ: {Dlq}, MaxRetries: {MaxRetries})",
            Settings.LrsQueueName, Settings.LrsDeadLetterQueueName, 3);
    }

    private async Task<bool> IsEnabledInDbAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var row = await db.SystemSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "LrsCredentials");
            if (row != null && !string.IsNullOrEmpty(row.Value))
            {
                var doc = JsonSerializer.Deserialize<JsonElement>(row.Value);
                if (doc.TryGetProperty("enabled", out var e) && e.GetBoolean())
                    return true;
            }
        }
        catch { /* fall through */ }
        return false;
    }

    private async Task<int> LoadMaxRetriesAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var row = await db.SystemSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "LrsCredentials");
            if (row != null && !string.IsNullOrEmpty(row.Value))
            {
                var doc = JsonSerializer.Deserialize<JsonElement>(row.Value);
                if (doc.TryGetProperty("maxRetries", out var mr))
                    return mr.GetInt32();
            }
        }
        catch { /* fall through */ }
        return 3;
    }

    private async Task<(string Endpoint, string Key, string Secret, string XApiVersion)?> LoadEffectiveCredentialsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var row = await db.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "LrsCredentials");

            if (row != null && !string.IsNullOrEmpty(row.Value))
            {
                var creds = JsonSerializer.Deserialize<JsonElement>(row.Value);
                var endpoint = creds.TryGetProperty("endpoint", out var ep) ? ep.GetString() : null;
                var key = creds.TryGetProperty("key", out var k) ? k.GetString() : null;
                var secret = creds.TryGetProperty("secret", out var s) ? s.GetString() : null;
                var version = creds.TryGetProperty("xApiVersion", out var v) ? v.GetString() : null;

                if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(secret))
                {
                    _logger.LogInformation("LRS credentials loaded from DB: endpoint={Endpoint}, key={Key}", endpoint, key);
                    return (endpoint, key, secret, version ?? "1.0.3");
                }

                _logger.LogWarning("LRS DB row found but credentials incomplete: endpoint={Endpoint}, key={Key}, secretEmpty={SecretEmpty}",
                    endpoint, key, string.IsNullOrEmpty(secret));
            }
            else
            {
                _logger.LogWarning("LRS credentials row not found in DB (key=LrsCredentials)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load LRS credentials from DB");
        }

        return null;
    }

    private async Task<(bool Success, int? HttpStatusCode, string? ErrorMessage)> SendToLrsAsync(
        string statementJson, CancellationToken ct)
    {
        var cred = await LoadEffectiveCredentialsAsync();
        if (cred == null)
            return (false, null, "LRS credentials not configured in database");

        var (endpoint, key, secret, xApiVersion) = cred.Value;

        _logger.LogInformation(
            "LRS sending to {Endpoint} as user={Key}, secret length={SecretLen}, xApiVersion={Ver}",
            endpoint, key, secret.Length, xApiVersion);

        var client = _httpClientFactory.CreateClient();
        var authBytes = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authBytes);
        client.DefaultRequestHeaders.Add("X-Experience-API-Version", xApiVersion);

        var content = new StringContent(statementJson, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(endpoint, content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        return (response.IsSuccessStatusCode, (int)response.StatusCode, responseBody);
    }

    private async Task RepublishForRetryAsync(byte[] body, int nextRetryCount)
    {
        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                [RetryCountHeader] = nextRetryCount
            }
        };

        await _channel!.BasicPublishAsync(
            exchange: "", routingKey: Settings.LrsQueueName,
            mandatory: false, basicProperties: properties, body: body);
    }

    private async Task PublishToDlqAsync(byte[] body, IReadOnlyBasicProperties? originalProps, string error)
    {
        var headers = new Dictionary<string, object?>();
        if (originalProps?.Headers != null)
            foreach (var pair in originalProps.Headers)
                headers[pair.Key] = pair.Value;

        headers[RetryCountHeader] = GetRetryCount(originalProps);
        headers[ErrorHeader] = Encoding.UTF8.GetBytes(error);
        headers[FailedAtHeader] = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O"));

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = headers
        };

        await _channel!.BasicPublishAsync(
            exchange: "", routingKey: Settings.LrsDeadLetterQueueName,
            mandatory: false, basicProperties: properties, body: body);
    }

    private async Task UpdateLogAsync(int? logId, LrsStatementStatus status, int? httpStatusCode, string? errorMessage, int? retryCount = null)
    {
        if (!logId.HasValue) return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            var log = await db.LrsStatementLogs.FindAsync(logId.Value);
            if (log == null) return;

            log.Status = status;
            log.HttpStatusCode = httpStatusCode ?? log.HttpStatusCode;
            log.ErrorMessage = errorMessage != null
                ? (errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage)
                : log.ErrorMessage;
            if (retryCount.HasValue) log.RetryCount = retryCount.Value;
            if (status == LrsStatementStatus.Sent) log.SentAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update LrsStatementLog {LogId} to {Status}", logId, status);
        }
    }

    private static int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers == null ||
            !properties.Headers.TryGetValue(RetryCountHeader, out var raw) ||
            raw is null)
            return 0;

        return raw switch
        {
            int i => i,
            long l => (int)l,
            byte b => b,
            short s => s,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => 0
        };
    }
}