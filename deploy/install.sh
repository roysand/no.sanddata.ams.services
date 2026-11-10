#!/usr/bin/env bash
# Installs or upgrades MqttForwarder as a systemd service. Run as root from the unpacked package:
#   sudo ./install.sh
# Safe to run again for an upgrade: the settings file and the outbox are kept.
set -euo pipefail

[ "$(id -u)" -eq 0 ] || { echo "Run as root: sudo ./install.sh" >&2; exit 1; }
command -v systemctl >/dev/null || { echo "systemd is required" >&2; exit 1; }

HERE="$(cd "$(dirname "$0")" && pwd)"
APP=/opt/mqttforwarder
SETTINGS="$APP/local.settings.json"

id mqttforwarder >/dev/null 2>&1 \
    || useradd --system --no-create-home --shell /usr/sbin/nologin mqttforwarder

systemctl stop mqttforwarder 2>/dev/null || true

# Replace the program files, keep the settings file.
mkdir -p "$APP"
find "$APP" -mindepth 1 -maxdepth 1 ! -name local.settings.json -exec rm -rf {} +
cp -a "$HERE/app/." "$APP/"
chmod 755 "$APP/MqttForwarder"
chown -R root:root "$APP"

# First install: create the settings file from the example. It holds the API key and the MQTT password,
# so only root and the service may read it.
[ -f "$SETTINGS" ] || cp "$HERE/local.settings.json.example" "$SETTINGS"
chown root:mqttforwarder "$SETTINGS"
chmod 640 "$SETTINGS"

install -m 644 "$HERE/mqttforwarder.service" /etc/systemd/system/mqttforwarder.service
systemctl daemon-reload
systemctl enable mqttforwarder >/dev/null

if grep -q -e 'your-api-key-here' -e '192.168.1.x' "$SETTINGS"; then
    echo
    echo "Installed. Now fill in $SETTINGS (MQTT server, API key), then run:"
    echo "  sudo systemctl start mqttforwarder"
    echo "  journalctl -u mqttforwarder -f"
else
    systemctl restart mqttforwarder
    sleep 2
    systemctl --no-pager --lines=15 status mqttforwarder || true
fi
