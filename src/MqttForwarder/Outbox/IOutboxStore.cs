using MqttForwarder.Contracts;

namespace MqttForwarder.Outbox;

/// <summary>
/// Local durable queue: every parsed reading is persisted here before anything touches the
/// network, so readings survive a home-internet outage. A drain loop later confirms delivery
/// to the AMS API and removes each entry only once accepted.
/// </summary>
public interface IOutboxStore
{
    Task EnqueueAsync(CanonicalReading reading, CancellationToken ct);

    Task<IReadOnlyList<OutboxEntry>> GetPendingAsync(int maxCount, CancellationToken ct);

    Task DeleteAsync(IReadOnlyCollection<long> ids, CancellationToken ct);
}
