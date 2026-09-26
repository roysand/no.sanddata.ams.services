using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MqttForwarder.Outbox;

namespace MqttForwarder.Forwarding;

public class AmsApiClient(HttpClient httpClient, ILogger<AmsApiClient> logger) : IAmsApiClient
{
    public async Task<bool> IngestAsync(string deviceId, IReadOnlyList<OutboxEntry> readings, CancellationToken ct)
    {
        var request = new IngestRequest(
            deviceId,
            readings.Select(r => new IngestReading(r.TimestampUnixSeconds, r.MeterId, r.MeterType, r.PowerWatts)).ToList());

        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/measurements", request, ct);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            string body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Ingestion rejected for {DeviceId}: {StatusCode} {Body}", deviceId, response.StatusCode, body);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogInformation(
                "AMS API is not reachable ({Reason}). Measurements for {DeviceId} are stored locally and will be uploaded once the API is back up.",
                ex.Message,
                deviceId);
            return false;
        }
    }

    private record IngestRequest([property: JsonPropertyName("deviceId")] string DeviceId,
        [property: JsonPropertyName("readings")] IReadOnlyList<IngestReading> Readings);

    private record IngestReading(
        [property: JsonPropertyName("timestamp")] long Timestamp,
        [property: JsonPropertyName("meterId")] string? MeterId,
        [property: JsonPropertyName("meterType")] string? MeterType,
        [property: JsonPropertyName("powerWatts")] int PowerWatts);
}
