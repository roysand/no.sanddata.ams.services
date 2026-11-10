# Installing the MqttForwarder at home

This guide takes you from nothing to readings arriving in the production database. It follows the real order:
the location and the meter must exist **before** the forwarder is started, and the sensor key is shown only once.

```
power meter ──► amsleser reader ──► MQTT broker ──► MqttForwarder (Linux, home) ──HTTPS──► ams-api.sanddata.eu ──► database
```

| Part | Where it runs |
|------|---------------|
| MqttForwarder | A Linux x64 machine at home, on the same network as the MQTT broker |
| AMS API, database | Hetzner (`https://ams-api.sanddata.eu`), already deployed |
| Web app (frontend) | Your development machine (`npm run dev`) |

## What you need

- [ ] A Linux x64 machine at home with **systemd** (Debian, Ubuntu and similar), root access (`sudo`), and a network
      path to both the MQTT broker and the internet (outgoing HTTPS only; no ports need to be opened).
- [ ] The MQTT broker's address, user name and password, and the topic the reader publishes on
      (the default in the forwarder is `iot/ams/home`).
- [ ] An account on production that may create locations (your administrator account, or any signed-in user).
- [ ] This repository and the .NET 10 SDK on your development machine, to build the package.

---

## 1. Create the location and the meter in the web app

### 1.1 Point the web app at production

The location must be created in the **production** database. In the frontend repository:

- `.env.development` must contain an **empty** `VITE_API_BASE_URL=` (that is the committed value). If it says
  `VITE_API_BASE_URL=http://localhost:5231`, the app talks to your *local* API and the location would be created in
  your local database. Remove that value.
- `API_PROXY_TARGET` defaults to `https://ams-api.sanddata.eu`. Only if you created a `.env`, check that it does not
  point to `https://localhost:7130`.

Then start it with `npm run dev` and open `http://localhost:5173`. Sign in with your **production** account. Your
local test accounts do not exist there.

### 1.2 Add the location (Hans Aanruds vei 43)

If you have no locations yet, the dashboard sends you to the setup wizard. Otherwise open **My locations** and use
**Add location**. Fill in:

| Field | Value |
|-------|-------|
| Name | `Hans Aanruds vei 43` (or a short name such as `Home`) |
| Address | `Hans Aanruds vei 43` |
| Serial number | Anything **unique** in the system, for example the electricity meter's serial number from its label, or `HAV43`. It cannot be changed by you later and no other location may use it. |
| Price zone | The zone for your address (`NO1` to `NO5`). Oslo and the rest of eastern Norway is `NO1`. It decides which spot prices are used for the cost figures. |
| Norgespris agreement | Tick only if you have a Norgespris agreement with your electricity supplier. |
| Active | Leave ticked. |

### 1.3 Copy the sensor key

After **Add location** the app shows the sensor key **once**.

- Press **Copy key** and paste it somewhere safe **right now**, for example straight into the settings file in step 4.
  Do not close the page before you have it.
- If it is lost, an administrator must generate a new key for the location, and the forwarder's settings must be
  updated.
- Treat the key as a password. Anyone who has it can write readings into this location.

### 1.4 Register the meter (the reader)

Step 2 of the wizard (or **Add meter** on the location's card) asks for:

| Field | Value |
|-------|-------|
| Location | The one you just created |
| Device id | The reader's id as it appears in its MQTT messages, for example `58:CF:79:9C:93:AE`. See step 2 below. |
| Comment | Optional, for example `Main meter` |

The device id must be **exactly** the `id` the reader publishes. A reading from a device id that is not registered at the
location is refused (`404 Meter.NotRegistered`). The forwarder keeps it in its outbox and tries again, so nothing
is lost, but no data arrives until the id matches.

---

## 2. Find the reader's device id and the MQTT details

The reader publishes JSON like this on the MQTT topic:

```json
{ "id": "58:CF:79:9C:93:AE", "t": 1787923271, "data": { "meterId": "7359992896383454", "type": "6525", ... } }
```

The device id is the `id` field. To see it, run this on any machine that can reach the broker
(`mosquitto-clients` package; `-C 1` stops after one message):

```bash
mosquitto_sub -h <broker-address> -p 1883 -u <user> -P '<password>' -t 'iot/ams/home' -C 1
```

If you would rather not install that, skip this step: start the forwarder with a wrong or missing meter and read the
device id from its log line `Ingestion rejected for meter ... (device <id>) ... Meter.NotRegistered`, then register that id.

---

## 3. Build the package (on your development machine)

From the root of this repository:

```bash
./deploy/publish.sh
```

This creates `artifacts/mqttforwarder-linux-x64.tar.gz`: a self-contained build (no .NET needed on the Linux machine)
with the installer and the service definition. It never contains your local settings or outbox.

Copy it to the Linux machine:

```bash
scp artifacts/mqttforwarder-linux-x64.tar.gz <user>@<linux-machine>:
```

---

## 4. Install it (on the Linux machine)

```bash
tar xzf mqttforwarder-linux-x64.tar.gz
cd mqttforwarder-linux-x64
sudo ./install.sh
```

The installer:

- creates the system user `mqttforwarder` (no login, no home directory);
- installs the program in `/opt/mqttforwarder` (owned by root, not writable by the service);
- creates `/opt/mqttforwarder/local.settings.json` from the example, readable only by root and the service;
- installs and enables the systemd unit `mqttforwarder`;
- keeps the outbox in `/var/lib/mqttforwarder/outbox.db`, outside the program folder.

On a first install it stops and tells you to fill in the settings. Edit the file:

```bash
sudo nano /opt/mqttforwarder/local.settings.json
```

```json
{
  "Mqtt": {
    "ServerUri": "192.168.1.50",
    "ServerPort": 1883,
    "UserName": "mqtt-user",
    "Password": "the-mqtt-password"
  },
  "AmsApi": {
    "BaseUrl": "https://ams-api.sanddata.eu",
    "ApiKey": "the-sensor-key-from-step-1.3"
  }
}
```

- `Mqtt:ServerUri`, `AmsApi:BaseUrl` and `AmsApi:ApiKey` are required; the service refuses to start without them.
- The topic defaults to `iot/ams/home`. To change it, add `"Topic": "your/topic"` to the `Mqtt` section.
- Use the broker's real address. If it uses TLS, also set `"UseTls": true` and the TLS port.

Start it and follow the log:

```bash
sudo systemctl start mqttforwarder
journalctl -u mqttforwarder -f
```

It starts automatically at boot, and systemd restarts it 5 seconds after a crash.

---

## 5. Check that data arrives

**On the Linux machine**

```bash
systemctl status mqttforwarder            # should say: active (running)
journalctl -u mqttforwarder --since "5 min ago"
```

The forwarder sends what it has collected every 30 seconds. A healthy log has no `Ingestion rejected` lines.

**In the web app:** open the dashboard and pick the new location; the power chart should start to fill with data.

**In the database (DataGrip, through the SSH tunnel, see `DockerDeployment.md` in the API repository):**

```sql
-- Readings per meter and when the latest one arrived
select l."Name", m."DeviceId", count(*) as readings,
       min(me."Timestamp") as first_reading, max(me."Timestamp") as last_reading
from "Measurement" me
join "Meter" m on m."Id" = me."MeterId"
join "Location" l on l."Id" = me."LocationId"
group by l."Name", m."DeviceId";

-- The latest readings
select me."Timestamp", me."PowerWatts"
from "Measurement" me
order by me."Timestamp" desc
limit 20;
```

The meter's own identity (`MeterId`, `MeterType` on the `Meter` row) is filled in automatically from the first reading.

---

## 6. Everyday operation

| Task | Command |
|------|---------|
| Status | `systemctl status mqttforwarder` |
| Live log | `journalctl -u mqttforwarder -f` |
| Restart (after editing the settings) | `sudo systemctl restart mqttforwarder` |
| Stop / start | `sudo systemctl stop mqttforwarder` / `sudo systemctl start mqttforwarder` |
| How many readings are waiting to be sent | `sudo sqlite3 /var/lib/mqttforwarder/outbox.db 'select count(*) from PendingReading'` (needs the `sqlite3` package) |

**Upgrade:** build a new package (step 3), copy it over, unpack it and run `sudo ./install.sh` again. The program is
replaced; the settings file and the outbox are kept, and the service is restarted.

**If the API or the network is down:** nothing is lost. The forwarder keeps every reading in the outbox and sends
them when the API is reachable again. The log says `AMS API is not reachable`.

**Remove it completely:**

```bash
sudo systemctl disable --now mqttforwarder
sudo rm /etc/systemd/system/mqttforwarder.service
sudo rm -r /opt/mqttforwarder /var/lib/mqttforwarder   # deletes the settings and any unsent readings
sudo userdel mqttforwarder
sudo systemctl daemon-reload
```

---

## 7. Troubleshooting

| What you see | Meaning and fix |
|--------------|-----------------|
| Service exits at once; the log mentions a required value or `OptionsValidationException` | `ServerUri`, `BaseUrl` or `ApiKey` is missing or empty in `local.settings.json`. Fix the file and restart. |
| `Ingestion rejected ... 401` | The sensor key is wrong, was rotated, is inactive or has expired, or the location is inactive. Copy the key again from a freshly generated one (an administrator can generate a new key), update the settings and restart. |
| `Ingestion rejected ... 404 ... Meter.NotRegistered` | The device id in the reader's messages is not registered at this location. Register the id shown in the log line (step 1.4). The waiting readings are then sent automatically. |
| `AMS API is not reachable` | No internet, DNS problem, or the server is down. Check from the machine: `curl -I https://ams-api.sanddata.eu`. Readings are kept meanwhile. |
| No log lines about readings at all | The forwarder is not receiving MQTT messages. Check the broker address, user and password, the topic (compare with `mosquitto_sub`, step 2), and that the reader is online. |
| Readings arrive but the dashboard is empty | Check that the location is **Active**, that you selected the right location, and that the meter's device id matches (step 5, first SQL query). |
| `Permission denied` when editing or reading the settings | The file is readable only by root and the service. Use `sudo`. |
| The location was created in the wrong database | You were pointing the web app at the local API (step 1.1). Create it again against production and use the new key. |

---

## Reference

| Item | Path or value |
|------|---------------|
| Program | `/opt/mqttforwarder/` (root-owned) |
| Settings (API key, MQTT login) | `/opt/mqttforwarder/local.settings.json` (mode 640, `root:mqttforwarder`) |
| Outbox (readings not yet sent) | `/var/lib/mqttforwarder/outbox.db` |
| Service definition | `/etc/systemd/system/mqttforwarder.service` |
| Log | `journalctl -u mqttforwarder` |
| API | `https://ams-api.sanddata.eu` (`POST /api/measurements`, header `X-API-Key`) |
| Sent every | 30 seconds, in batches of up to 50 readings per device |

The settings can also be given as environment variables (`Mqtt__ServerUri`, `AmsApi__ApiKey`, and so on), for example in a
systemd drop-in, if you prefer that to the JSON file.
