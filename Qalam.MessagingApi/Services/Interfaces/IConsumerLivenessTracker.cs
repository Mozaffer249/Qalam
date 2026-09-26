namespace Qalam.MessagingApi.Services.Interfaces;

/// <summary>
/// Tracks whether each RabbitMQ consumer hosted service is currently connected and listening.
/// Used by the health endpoint so Docker can restart the process if the email consumer dies.
/// </summary>
public interface IConsumerLivenessTracker
{
    void SetConnected(string consumerName, bool connected);

    bool IsConnected(string consumerName);

    IReadOnlyDictionary<string, bool> Snapshot();
}
