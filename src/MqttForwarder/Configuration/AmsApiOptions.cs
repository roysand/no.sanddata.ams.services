using System.ComponentModel.DataAnnotations;

namespace MqttForwarder.Configuration;

public class AmsApiOptions
{
    public const string SectionName = "AmsApi";

    [Required]
    public required string BaseUrl { get; init; }

    [Required]
    public required string ApiKey { get; init; }
}
