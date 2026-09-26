using System.Text.Json;
using Microsoft.Extensions.Logging;
using MqttForwarder.Contracts;

namespace MqttForwarder.Parsing;

/// <summary>Parses the amsleser.no Pow-U reader's MQTT payload (Aidon meter, v1 scope: power only).</summary>
public class AidonMeterReadingParser(ILogger<AidonMeterReadingParser> logger) : IMeterReadingParser
{
    public CanonicalReading? Parse(ReadOnlySpan<byte> payload)
    {
        AmsReaderData? reading;
        try
        {
            reading = JsonSerializer.Deserialize<AmsReaderData>(payload);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize MQTT payload, skipping message");
            return null;
        }

        if (reading?.Data is null || string.IsNullOrEmpty(reading.Id))
        {
            return null;
        }

        return new CanonicalReading(
            reading.Id,
            reading.TimestampUnixSeconds,
            reading.Data.MeterId,
            reading.Data.Type,
            reading.Data.PowerWatts);
    }
}
