using MqttForwarder.Outbox;

namespace MqttForwarder.Forwarding;

/// <summary>What the API tells us about the location/meter a batch was accepted for.</summary>
public record IngestResult(string? LocationName, string? MeterComment)
{
    /// <summary>Human-readable label, e.g. "Cabin - Main meter"; null if the API supplied neither.</summary>
    public string? Label => (LocationName, MeterComment) switch
    {
        (null or "", null or "") => null,
        (var l, null or "") => l,
        (null or "", var m) => m,
        var (l, m) => $"{l} - {m}"
    };
}

public interface IAmsApiClient
{
    /// <summary>Posts a batch of readings for one device. Returns the API's result on a confirmed 200 OK, otherwise null.</summary>
    Task<IngestResult?> IngestAsync(string deviceId, IReadOnlyList<OutboxEntry> readings, CancellationToken ct);
}
