using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Qalam.MessagingApi.Configuration;
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
    private readonly LrsSettings _lrsSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private IChannel? _channel;

    public LrsStatementConsumer(
        ILogger<LrsStatementConsumer> logger,
        IOptions<RabbitMQSettings> rabbitSettings,
        IOptions<LrsSettings> lrsSettings,
        IHttpClientFactory httpClientFactory,
        IConsumerLivenessTracker liveness)
        : base(rabbitSettings, liveness, logger)
    {
        _logger = logger;
        _lrsSettings = lrsSettings.Value;
        _httpClientFactory = httpClientFactory;
    }

    protected override string ConsumerName => LivenessName;

    protected override async Task SetupAndConsumeAsync(IChannel channel, CancellationToken stoppingToken)
    {
        if (!_lrsSettings.Enabled)
        {
            _logger.LogInformation("LRS consumer disabled (LrsSettings.Enabled=false). Idling.");
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
                        "LRS statement sent: Verb={Verb}, Actor={Actor}, Object={Object}",
                        message.Verb, message.ActorName, message.ObjectId);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else if (result.HttpStatusCode.HasValue && result.HttpStatusCode.Value >= 400 && result.HttpStatusCode.Value < 500)
                {
                    _logger.LogWarning(
                        "LRS permanent failure ({StatusCode}), moving to DLQ: {Error}",
                        result.HttpStatusCode, result.ErrorMessage);
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

                var maxRetries = Math.Max(0, _lrsSettings.MaxRetries);
                var payload = ea.Body.ToArray();

                if (retryCount >= maxRetries)
                {
                    _logger.LogWarning(
                        "LRS max retries reached ({Retry}/{Max}), moving to DLQ",
                        retryCount, maxRetries);
                    await PublishToDlqAsync(payload, ea.BasicProperties, ex.Message);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else
                {
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
            Settings.LrsQueueName, Settings.LrsDeadLetterQueueName, _lrsSettings.MaxRetries);
    }

    private async Task<(bool Success, int? HttpStatusCode, string? ErrorMessage)> SendToLrsAsync(
        string statementJson, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("NelcLrs");
        var content = new StringContent(statementJson, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(_lrsSettings.Endpoint, content, ct);
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
