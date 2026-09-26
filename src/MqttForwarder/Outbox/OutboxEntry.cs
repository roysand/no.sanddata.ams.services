namespace MqttForwarder.Outbox;

public record OutboxEntry(long Id, string DeviceId, long TimestampUnixSeconds, string? MeterId, string? MeterType, int PowerWatts);
