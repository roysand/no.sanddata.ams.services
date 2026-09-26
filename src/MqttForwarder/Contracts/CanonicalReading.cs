namespace MqttForwarder.Contracts;

/// <summary>Reader-agnostic reading shape, matching the AMS API's ingestion contract.</summary>
public record CanonicalReading(string DeviceId, long TimestampUnixSeconds, string? MeterId, string? MeterType, int PowerWatts);
