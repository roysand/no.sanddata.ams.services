using MqttForwarder.Contracts;

namespace MqttForwarder.Parsing;

/// <summary>
/// Converts a raw MQTT payload into the canonical reading shape the AMS API accepts.
/// Swapping readers later means writing a new implementation of this interface, without
/// touching any MQTT plumbing or outbox/forwarding code.
/// </summary>
public interface IMeterReadingParser
{
    /// <summary>Returns null when the payload has no usable reading (e.g. a status-only message).</summary>
    CanonicalReading? Parse(ReadOnlySpan<byte> payload);
}
