# System Monitor

A real-time system monitoring solution. Sensors collect CPU and memory metrics from remote machines and stream them to a central agent, which persists the data and pushes live updates to a web dashboard.

## Architecture

```
┌─────────────────┐                         ┌──────────────────────┐
│  Sensor(s)      │  ──── measurements ───► │  Agent               │
│  (Worker Svc)   │        TCP :5100        │  (ASP.NET Core API)  │
│                 │  ◄─ config updates ───  │  HTTP  :5000         │
└─────────────────┘                         │  SQLite DB           │
                                            └──────────┬───────────┘
                                                       │ SignalR
                                                       ▼
                                            ┌──────────────────────┐
                                            │  Dashboard           │
                                            │  (Blazor WASM)       │
                                            │  HTTP  :5069         │
                                            └──────────────────────┘
```

## Components

| Project | Type | Description |
|---|---|---|
| `SystemMonitor.Agent` | ASP.NET Core Web API | Accepts sensor connections over TCP, stores measurements in SQLite, serves a REST API and SignalR hubs for the dashboard |
| `SystemMonitor.Sensor` | .NET Worker Service | Collects CPU and memory metrics, buffers them in a circular buffer, and forwards them to the Agent over TCP. Runs as a Windows Service or systemd unit |
| `SystemMonitor.Dashboard` | Blazor WebAssembly | Real-time dashboard with sensor cards, a paginated measurements table, JWT login, and a dark/light theme toggle |
| `SystemMonitor.Shared` | Class Library | DTOs and contracts shared between Agent and Dashboard |

## Dashboard features

- Live sensor cards — connection state, current CPU & memory readings, configurable measurement period
- Measurements table — paginated, filterable by sensor ID and time range, full-height layout
- Per-sensor detail dialog — last 10 measurements, live connection state
- Dark mode by default, switchable to light mode (persisted in `localStorage`)

## Configuration

**Agent** (`src/SystemMonitor.Agent/appsettings.json`)

| Key | Default | Description |
|---|---|---|
| `TcpConfiguration:Port` | `5100` | Port that sensors connect to |
| `ConnectionStrings:ApplicationDatabaseContext` | `../../Database.db` | SQLite database path |
| `JwtConfiguration` | — | Secret key, issuer, audience for JWT auth |

**Sensor** (`src/SystemMonitor.Sensor/appsettings.json`)

| Key | Default | Description |
|---|---|---|
| `AgentConfiguration:Host` | `127.0.0.1` | Agent IP address |
| `AgentConfiguration:Port` | `5100` | Agent TCP port |
| `BufferConfiguration:BufferCapacity` | `100` | Circular buffer size (measurements) |

## Running

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows or Linux (Ubuntu)

### Windows

Open PowerShell **as Administrator** (required for Windows Service registration), then run:

```powershell
.\Startup.ps1
```

To skip the build step when nothing has changed:

```powershell
.\Startup.ps1 -SkipBuild
```

This publishes all three projects to `publish\`, registers the Sensor as a Windows Service (`SystemMonitorSensor`), then starts the Agent, Dashboard, and Sensor in order.

### Linux (Ubuntu)

```bash
chmod +x startup.sh
sudo ./startup.sh
```

To skip the build step:

```bash
sudo ./startup.sh --skip-build
```

This publishes all three projects, writes a systemd unit file to `/etc/systemd/system/SystemMonitorSensor.service`, then starts the Agent, Dashboard, and Sensor in order.

### What the scripts start

| Service | How it runs | URL / command |
|---|---|---|
| Agent | Published executable | `http://localhost:5000` |
| Dashboard | `dotnet run` (Blazor dev server) | `http://localhost:5069` |
| Sensor | Windows Service / systemd unit | `sc query SystemMonitorSensor` / `systemctl status SystemMonitorSensor` |

### Logs

| Component | Location |
|---|---|
| Agent | `publish/Agent/logs/agent-YYYYMMDD.log` |
| Sensor | `publish/Sensor/logs/sensor-YYYYMMDD.log` |
| Dashboard (Linux) | `publish/dashboard.log` |

### First run — register an account

Navigate to `http://localhost:5069`, register an account, then log in. The Agent's Swagger UI is available at `http://localhost:5000/swagger`.
