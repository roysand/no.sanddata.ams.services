using MqttForwarder.Outbox;

namespace MqttForwarder.Forwarding;

public interface IAmsApiClient
{
    /// <summary>Posts a batch of readings for one device. Returns true only on a confirmed 200 OK.</summary>
    Task<bool> IngestAsync(string deviceId, IReadOnlyList<OutboxEntry> readings, CancellationToken ct);
}
