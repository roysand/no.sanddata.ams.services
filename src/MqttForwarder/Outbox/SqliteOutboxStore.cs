using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MqttForwarder.Configuration;
using MqttForwarder.Contracts;

namespace MqttForwarder.Outbox;

public class SqliteOutboxStore : IOutboxStore
{
    private readonly string _connectionString;

    public SqliteOutboxStore(IOptions<OutboxOptions> options)
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = options.Value.DbPath }.ToString();
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS PendingReading (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DeviceId TEXT NOT NULL,
                TimestampUnixSeconds INTEGER NOT NULL,
                MeterId TEXT NULL,
                MeterType TEXT NULL,
                PowerWatts INTEGER NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public async Task EnqueueAsync(CanonicalReading reading, CancellationToken ct)
    {
        await using SqliteConnection connection = OpenConnection();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO PendingReading (DeviceId, TimestampUnixSeconds, MeterId, MeterType, PowerWatts)
            VALUES ($deviceId, $timestamp, $meterId, $meterType, $powerWatts);
            """;
        command.Parameters.AddWithValue("$deviceId", reading.DeviceId);
        command.Parameters.AddWithValue("$timestamp", reading.TimestampUnixSeconds);
        command.Parameters.AddWithValue("$meterId", (object?)reading.MeterId ?? DBNull.Value);
        command.Parameters.AddWithValue("$meterType", (object?)reading.MeterType ?? DBNull.Value);
        command.Parameters.AddWithValue("$powerWatts", reading.PowerWatts);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<OutboxEntry>> GetPendingAsync(int maxCount, CancellationToken ct)
    {
        await using SqliteConnection connection = OpenConnection();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, DeviceId, TimestampUnixSeconds, MeterId, MeterType, PowerWatts
            FROM PendingReading
            ORDER BY Id
            LIMIT $maxCount;
            """;
        command.Parameters.AddWithValue("$maxCount", maxCount);

        var entries = new List<OutboxEntry>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            entries.Add(new OutboxEntry(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt64(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetInt32(5)));
        }

        return entries;
    }

    public async Task DeleteAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return;
        }

        await using SqliteConnection connection = OpenConnection();
        await using SqliteCommand command = connection.CreateCommand();
        string placeholders = string.Join(",", ids.Select((_, i) => $"${i}"));
        command.CommandText = $"DELETE FROM PendingReading WHERE Id IN ({placeholders});";
        int i2 = 0;
        foreach (long id in ids)
        {
            command.Parameters.AddWithValue($"${i2}", id);
            i2++;
        }

        await command.ExecuteNonQueryAsync(ct);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
