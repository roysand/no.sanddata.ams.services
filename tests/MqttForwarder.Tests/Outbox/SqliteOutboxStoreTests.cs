using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MqttForwarder.Configuration;
using MqttForwarder.Contracts;
using MqttForwarder.Outbox;

namespace MqttForwarder.Tests.Outbox;

public class SqliteOutboxStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"outbox-test-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task EnqueueThenGetPending_ReturnsEnqueuedReadingInOrder()
    {
        var store = new SqliteOutboxStore(Options.Create(new OutboxOptions { DbPath = _dbPath }));
        var reading = new CanonicalReading("58:CF:79:9C:93:AE", 1790435940, "7359992896383454", "6525", 1028);

        await store.EnqueueAsync(reading, CancellationToken.None);
        IReadOnlyList<OutboxEntry> pending = await store.GetPendingAsync(10, CancellationToken.None);

        OutboxEntry entry = Assert.Single(pending);
        Assert.Equal(reading.DeviceId, entry.DeviceId);
        Assert.Equal(reading.TimestampUnixSeconds, entry.TimestampUnixSeconds);
        Assert.Equal(reading.MeterId, entry.MeterId);
        Assert.Equal(reading.MeterType, entry.MeterType);
        Assert.Equal(reading.PowerWatts, entry.PowerWatts);
    }

    [Fact]
    public async Task Delete_RemovesEntrySoItIsNotReturnedAgain()
    {
        var store = new SqliteOutboxStore(Options.Create(new OutboxOptions { DbPath = _dbPath }));
        await store.EnqueueAsync(new CanonicalReading("device-1", 1000, null, null, 500), CancellationToken.None);
        IReadOnlyList<OutboxEntry> pending = await store.GetPendingAsync(10, CancellationToken.None);

        await store.DeleteAsync([pending[0].Id], CancellationToken.None);
        IReadOnlyList<OutboxEntry> afterDelete = await store.GetPendingAsync(10, CancellationToken.None);

        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task GetPendingAsync_RespectsMaxCount()
    {
        var store = new SqliteOutboxStore(Options.Create(new OutboxOptions { DbPath = _dbPath }));
        for (int i = 0; i < 5; i++)
        {
            await store.EnqueueAsync(new CanonicalReading("device-1", 1000 + i, null, null, 500), CancellationToken.None);
        }

        IReadOnlyList<OutboxEntry> pending = await store.GetPendingAsync(2, CancellationToken.None);

        Assert.Equal(2, pending.Count);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite pools connections by default, which keeps a file handle open
        // even after each connection is disposed - clear the pool first or deletion fails.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
