using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Services.Interfaces;
using RabbitMQ.Client;

namespace Qalam.MessagingApi.BackgroundServices;

/// <summary>
/// Connects to RabbitMQ with automatic recovery and an outer reconnect loop so a brief
/// broker outage cannot leave consumers dead forever while the HTTP process stays healthy.
/// </summary>
public abstract class RabbitMqConsumerBase : BackgroundService
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan OpenPollInterval = TimeSpan.FromSeconds(5);
    private const int ClosedPollsBeforeReconnect = 3;

    private readonly IConsumerLivenessTracker _liveness;
    private readonly ILogger _logger;

    protected RabbitMqConsumerBase(
        IOptions<RabbitMQSettings> rabbitSettings,
        IConsumerLivenessTracker liveness,
        ILogger logger)
    {
        Settings = rabbitSettings.Value;
        _liveness = liveness;
        _logger = logger;
    }

    /// <summary>Stable name reported to <see cref="IConsumerLivenessTracker"/> (e.g. "Email").</summary>
    protected abstract string ConsumerName { get; }

    protected RabbitMQSettings Settings { get; }

    /// <summary>
    /// Declare queues, attach consumers, and call <c>BasicConsumeAsync</c>.
    /// Do not block forever here — the base waits for disconnect / cancel.
    /// </summary>
    protected abstract Task SetupAndConsumeAsync(IChannel channel, CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Consumer} starting...", ConsumerName);
        var backoff = MinBackoff;

        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? connection = null;
            IChannel? channel = null;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = Settings.HostName,
                    Port = Settings.Port,
                    UserName = Settings.UserName,
                    Password = Settings.Password,
                    VirtualHost = Settings.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                    TopologyRecoveryEnabled = true
                };

                connection = await factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await SetupAndConsumeAsync(channel, stoppingToken);

                _liveness.SetConnected(ConsumerName, true);
                backoff = MinBackoff;
                _logger.LogInformation("{Consumer} connected and listening", ConsumerName);

                await WaitUntilDisconnectedAsync(connection, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogWarning("{Consumer} RabbitMQ connection lost; will reconnect", ConsumerName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Consumer} encountered an error; reconnecting in {DelaySeconds}s",
                    ConsumerName, backoff.TotalSeconds);
            }
            finally
            {
                _liveness.SetConnected(ConsumerName, false);
                await SafeDisposeAsync(channel, connection);
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await Task.Delay(backoff, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var nextSeconds = Math.Min(backoff.TotalSeconds * 2, MaxBackoff.TotalSeconds);
            backoff = TimeSpan.FromSeconds(nextSeconds);
        }

        _liveness.SetConnected(ConsumerName, false);
        _logger.LogInformation("{Consumer} stopping...", ConsumerName);
    }

    private async Task WaitUntilDisconnectedAsync(IConnection connection, CancellationToken stoppingToken)
    {
        // Poll IsOpen so AutomaticRecovery can restore a brief blip without tearing down
        // the consumer. Only full-reconnect after sustained closed state.
        var consecutiveClosed = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(OpenPollInterval, stoppingToken);

            if (connection.IsOpen)
            {
                consecutiveClosed = 0;
                continue;
            }

            consecutiveClosed++;
            if (consecutiveClosed >= ClosedPollsBeforeReconnect)
                return;
        }
    }

    private static async Task SafeDisposeAsync(IChannel? channel, IConnection? connection)
    {
        if (channel != null)
        {
            try
            {
                if (channel.IsOpen)
                    await channel.CloseAsync();
            }
            catch
            {
                // ignore close races during broker restart
            }

            try
            {
                await channel.DisposeAsync();
            }
            catch
            {
                // ignore
            }
        }

        if (connection != null)
        {
            try
            {
                if (connection.IsOpen)
                    await connection.CloseAsync();
            }
            catch
            {
                // ignore
            }

            try
            {
                await connection.DisposeAsync();
            }
            catch
            {
                // ignore
            }
        }
    }
}
