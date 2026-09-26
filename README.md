# no.sanddata.ams.services

MQTT-to-AMS-API bridge for home energy meter (AMS/HAN) readings.

## What it does

`MqttForwarder` is a .NET worker service that:

1. Subscribes to an MQTT topic published by an [amsleser.no](https://amsleser.no) Pow-U reader (Aidon meter, power-only in v1).
2. Parses each incoming payload into a reader-agnostic `CanonicalReading`.
3. Writes every reading to a local SQLite-backed outbox for durability.
4. Periodically drains the outbox, grouping pending readings by device and POSTing them to the AMS API (`/api/measurements`). Entries are only deleted from the outbox once the API confirms ingestion — anything that fails (offline, API down) stays queued and is retried on the next drain cycle.

This design means the service tolerates MQTT broker or AMS API downtime without losing readings.

## Project layout

```
src/MqttForwarder/
  MqttSubscriberService.cs   # MQTT subscription, reconnect handling
  Parsing/                   # Reader payload -> CanonicalReading
  Outbox/                    # SQLite-backed durable queue
  OutboxDrainService.cs      # Periodic drain loop, groups by device
  Forwarding/                # HTTP client for the AMS API
  Configuration/             # Strongly-typed, validated options
tests/MqttForwarder.Tests/   # xUnit tests for parsing and outbox
```

## Configuration

Settings are bound from `appsettings.json` / `appsettings.Development.json`, with an optional `local.settings.json` (gitignored) for machine-local secrets and overrides.

```json
{
  "Mqtt": {
    "Topic": "iot/ams/home",
    "ServerUri": "",
    "ServerPort": 1883,
    "UseTls": false,
    "UserName": null,
    "Password": null
  },
  "AmsApi": {
    "BaseUrl": "",
    "ApiKey": ""
  },
  "Outbox": {
    "DbPath": "outbox.db",
    "DrainIntervalSeconds": 30,
    "BatchSize": 50
  }
}
```

- `Mqtt:ServerUri`, `AmsApi:BaseUrl` and `AmsApi:ApiKey` are required and validated on startup.
- The AMS API key is sent as the `X-API-Key` request header.

## Running

```
dotnet run --project src/MqttForwarder
```

## Testing

```
dotnet test
```
