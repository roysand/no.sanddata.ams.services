using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttForwarder.Configuration;
using MqttForwarder.Forwarding;
using MqttForwarder.Outbox;

namespace MqttForwarder;

/// <summary>
/// Periodically drains the local outbox, POSTing pending readings to the AMS API and deleting
/// each entry only once it's confirmed accepted. Anything that fails (offline, API down) simply
/// stays in the outbox and is retried on the next tick.
/// </summary>
public class OutboxDrainService(
    IOutboxStore outbox,
    IAmsApiClient apiClient,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDrainService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.DrainIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox drain cycle failed unexpectedly");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task DrainOnceAsync(CancellationToken ct)
    {
        IReadOnlyList<OutboxEntry> pending = await outbox.GetPendingAsync(options.Value.BatchSize, ct);
        if (pending.Count == 0)
        {
            return;
        }

        foreach (IGrouping<string, OutboxEntry> group in pending.GroupBy(e => e.DeviceId))
        {
            List<OutboxEntry> readings = group.ToList();
            bool accepted = await apiClient.IngestAsync(group.Key, readings, ct);
            if (accepted)
            {
                await outbox.DeleteAsync(readings.Select(r => r.Id).ToList(), ct);
                logger.LogInformation("Forwarded {Count} readings for {DeviceId}", readings.Count, group.Key);
            }
        }
    }
}
