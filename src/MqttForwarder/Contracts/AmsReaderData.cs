using System.Text.Json.Serialization;

namespace MqttForwarder.Contracts;

/// <summary>Raw payload published by the amsleser.no Pow-U reader on its MQTT topic.</summary>
public class AmsReaderData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("t")]
    public long TimestampUnixSeconds { get; set; }

    [JsonPropertyName("data")]
    public AmsReaderDataDetail? Data { get; set; }
}

public class AmsReaderDataDetail
{
    [JsonPropertyName("meterId")]
    public string? MeterId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("P")]
    public int PowerWatts { get; set; }
}
