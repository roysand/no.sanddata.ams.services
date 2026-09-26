using System.ComponentModel.DataAnnotations;

namespace MqttForwarder.Configuration;

public class MqttOptions
{
    public const string SectionName = "Mqtt";

    [Required]
    public required string Topic { get; init; }

    [Required]
    public required string ServerUri { get; init; }
    public int ServerPort { get; init; } = 1883;
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public bool UseTls { get; init; }
}
