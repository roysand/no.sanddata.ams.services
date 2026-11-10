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

## Running as a service (Linux x64, systemd)

The package is self-contained: the target machine needs no .NET install. It must be able to reach the MQTT broker
and the AMS API.

**1. Create the location, meter and sensor key in the web app** (signed in as the owner):

- Add the location. The sensor key is shown **once**; copy it.
- Register the reader as a meter. Its device id is the `id` the reader publishes in its MQTT payload
  (like `58:CF:79:9C:93:AE`). A batch from an unregistered device is refused with `Meter.NotRegistered` and stays in
  the outbox until the meter exists, so a wrong id only delays the data.

**2. Build the package** (on the development machine):

```
./deploy/publish.sh          # creates artifacts/mqttforwarder-linux-x64.tar.gz
scp artifacts/mqttforwarder-linux-x64.tar.gz <user>@<machine>:
```

**3. Install it** (on the target machine):

```
tar xzf mqttforwarder-linux-x64.tar.gz
cd mqttforwarder-linux-x64
sudo ./install.sh
sudo nano /opt/mqttforwarder/local.settings.json     # MQTT server and login, AmsApi:ApiKey
sudo systemctl start mqttforwarder
journalctl -u mqttforwarder -f
```

`local.settings.json` is created from the example on the first install (`AmsApi:BaseUrl` is
`https://ams-api.sanddata.eu`), readable only by root and the service. Running `install.sh` again upgrades the
program and keeps both the settings file and the outbox (`/var/lib/mqttforwarder/outbox.db`). If the settings
are already filled in, the script starts the service itself.

Useful commands: `systemctl status mqttforwarder`, `sudo systemctl restart mqttforwarder` (after changing the
settings), `journalctl -u mqttforwarder --since "10 min ago"`. A line like "AMS API is not reachable" means readings
are being kept locally; "Ingestion rejected ... 401" means a wrong or inactive sensor key, and "404 ... Meter.NotRegistered"
means the device id is not registered at that location.

## Testing

```
dotnet test
```
