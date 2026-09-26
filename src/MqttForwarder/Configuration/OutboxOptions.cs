namespace MqttForwarder.Configuration;

public class OutboxOptions
{
    public const string SectionName = "Outbox";

    public string DbPath { get; init; } = "outbox.db";
    public int DrainIntervalSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 50;
}
