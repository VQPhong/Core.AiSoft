using System;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace Core.AiSoft.Blazor.InfraCheck;

/// <summary>
/// Remembers which pings came back through RabbitMQ (in memory, per process).
/// </summary>
public class InfraPingTracker : ISingletonDependency
{
    private readonly ConcurrentDictionary<Guid, DateTime> _received = new();

    public void MarkReceived(Guid pingId, DateTime receivedAt)
    {
        _received[pingId] = receivedAt;
    }

    public DateTime? GetReceivedAt(Guid pingId)
    {
        return _received.TryGetValue(pingId, out var receivedAt) ? receivedAt : null;
    }
}
