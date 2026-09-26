using System.Collections.Concurrent;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

public sealed class ConsumerLivenessTracker : IConsumerLivenessTracker
{
    private readonly ConcurrentDictionary<string, bool> _states = new(StringComparer.OrdinalIgnoreCase);

    public void SetConnected(string consumerName, bool connected) =>
        _states[consumerName] = connected;

    public bool IsConnected(string consumerName) =>
        _states.TryGetValue(consumerName, out var connected) && connected;

    public IReadOnlyDictionary<string, bool> Snapshot() =>
        new Dictionary<string, bool>(_states, StringComparer.OrdinalIgnoreCase);
}
