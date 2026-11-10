#!/usr/bin/env bash
# Builds a self-contained linux-x64 package (no .NET install needed on the target machine):
#   artifacts/mqttforwarder-linux-x64.tar.gz
# Copy it to the machine, unpack it and run install.sh there (see README, "Running as a service").
set -euo pipefail
cd "$(dirname "$0")/.."

NAME=mqttforwarder-linux-x64
OUT="artifacts/$NAME"
rm -rf "$OUT" "artifacts/$NAME.tar.gz"

dotnet publish src/MqttForwarder -c Release -r linux-x64 --self-contained true -o "$OUT/app"

# Never ship secrets or local data, whatever the project file says.
rm -f "$OUT/app/local.settings.json" "$OUT/app/outbox.db"

cp deploy/install.sh deploy/mqttforwarder.service src/MqttForwarder/local.settings.json.example "$OUT/"
chmod +x "$OUT/install.sh"

COPYFILE_DISABLE=1 tar -C artifacts -czf "artifacts/$NAME.tar.gz" "$NAME"
echo "Created artifacts/$NAME.tar.gz"
