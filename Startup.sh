#!/usr/bin/env bash
# Requires root (sudo) for systemd registration.
set -euo pipefail

# ─── Args ─────────────────────────────────────────────────────────────────────

SKIP_BUILD=false
for arg in "$@"; do
    [[ "$arg" == "--skip-build" ]] && SKIP_BUILD=true
done

# ─── Paths ────────────────────────────────────────────────────────────────────

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="$SCRIPT_DIR/src"
OUT="$SCRIPT_DIR/publish"

AGENT_SRC="$SRC/SystemMonitor.Agent"
DASHBOARD_SRC="$SRC/SystemMonitor.Dashboard"
SENSOR_SRC="$SRC/SystemMonitor.Sensor"

AGENT_OUT="$OUT/Agent"
DASHBOARD_OUT="$OUT/Dashboard"
SENSOR_OUT="$OUT/Sensor"

# No .exe on Linux — dotnet publish produces a plain binary via the AppHost
AGENT_BIN="$AGENT_OUT/SystemMonitor.Agent"
SENSOR_BIN="$SENSOR_OUT/SystemMonitor.Sensor"

# Must match AddSystemd / AddWindowsService name in Sensor/Program.cs
SERVICE_NAME="SystemMonitorSensor"
SERVICE_FILE="/etc/systemd/system/$SERVICE_NAME.service"

# If run with sudo, start Agent & Dashboard as the original user, not root
RUN_AS="${SUDO_USER:-$(whoami)}"

# ─── Helpers ──────────────────────────────────────────────────────────────────

step() { echo; echo "==> $1"; }

require_root() {
    if [[ $EUID -ne 0 ]]; then
        echo "Error: systemd registration requires root. Run with sudo." >&2
        exit 1
    fi
}

# ─────────────────────────────────────────────────────────────────────────────
# 1. Build
# ─────────────────────────────────────────────────────────────────────────────

if [[ "$SKIP_BUILD" == "false" ]]; then
    step "Building Agent"
    dotnet publish "$AGENT_SRC" -c Release -o "$AGENT_OUT" --nologo

    step "Building Dashboard"
    dotnet publish "$DASHBOARD_SRC" -c Release -o "$DASHBOARD_OUT" --nologo

    step "Building Sensor"
    dotnet publish "$SENSOR_SRC" -c Release -o "$SENSOR_OUT" --nologo
fi

chmod +x "$AGENT_BIN" "$SENSOR_BIN"

# ─────────────────────────────────────────────────────────────────────────────
# 2. Register Sensor as systemd service
# ─────────────────────────────────────────────────────────────────────────────

require_root

step "Registering Sensor systemd service"

# Stop the service before rewriting the unit file
if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
    echo "  Stopping running instance..."
    systemctl stop "$SERVICE_NAME"
fi

cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=System Monitor Sensor
After=network.target

[Service]
Type=notify
ExecStart=$SENSOR_BIN
WorkingDirectory=$SENSOR_OUT
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
echo "  Unit file written to $SERVICE_FILE"

# ─────────────────────────────────────────────────────────────────────────────
# 3. Start Agent
# ─────────────────────────────────────────────────────────────────────────────

step "Starting Agent  (http://localhost:5000)"

# Kill any previous instance
pkill -f "SystemMonitor.Agent" 2>/dev/null || true

sudo -u "$RUN_AS" nohup "$AGENT_BIN" --urls "http://localhost:5000" \
    > "$OUT/agent.log" 2>&1 &
echo "  Agent started (PID $!, log: $OUT/agent.log)"

echo "  Waiting for Agent to initialise..."
sleep 3

# ─────────────────────────────────────────────────────────────────────────────
# 4. Start Dashboard
# ─────────────────────────────────────────────────────────────────────────────

step "Starting Dashboard  (http://localhost:5069)"

# Blazor WASM requires the ASP.NET Core dev server; published output is static
# files only. --no-build reuses the Release artifacts from the publish step.
pkill -f "dotnet.*run.*SystemMonitor.Dashboard" 2>/dev/null || true

sudo -u "$RUN_AS" nohup dotnet run \
    --project "$DASHBOARD_SRC" \
    -c Release \
    --no-build \
    --urls "http://localhost:5069" \
    > "$OUT/dashboard.log" 2>&1 &
echo "  Dashboard started (PID $!, log: $OUT/dashboard.log)"

# ─────────────────────────────────────────────────────────────────────────────
# 5. Start Sensor
# ─────────────────────────────────────────────────────────────────────────────

step "Starting Sensor  (systemd: $SERVICE_NAME)"
systemctl start "$SERVICE_NAME"

echo
echo "All services are running."
echo "  Agent     : http://localhost:5000  (log: $OUT/agent.log)"
echo "  Dashboard : http://localhost:5069  (log: $OUT/dashboard.log)"
echo "  Sensor    : systemctl status $SERVICE_NAME"
