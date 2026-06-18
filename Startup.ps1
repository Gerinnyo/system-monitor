#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Builds, registers, and starts all System Monitor components.
.PARAMETER SkipBuild
    Skip the publish step and use existing output in .\publish\.
#>
param([switch]$SkipBuild)

$ErrorActionPreference = "Stop"

$Root   = $PSScriptRoot
$Src    = Join-Path $Root "src"
$Out    = Join-Path $Root "publish"

$AgentSrc     = Join-Path $Src "SystemMonitor.Agent"
$DashboardSrc = Join-Path $Src "SystemMonitor.Dashboard"
$SensorSrc    = Join-Path $Src "SystemMonitor.Sensor"

$AgentOut     = Join-Path $Out "Agent"
$DashboardOut = Join-Path $Out "Dashboard"
$SensorOut    = Join-Path $Out "Sensor"

$AgentExe     = Join-Path $AgentOut  "SystemMonitor.Agent.exe"
$SensorExe    = Join-Path $SensorOut "SystemMonitor.Sensor.exe"

# Must match AddWindowsService(o => o.ServiceName = "...") in Sensor/Program.cs
$ServiceName = "SystemMonitorSensor"

function Write-Step([string]$Text) {
    Write-Host ""
    Write-Host "==> $Text" -ForegroundColor Cyan
}

# ─────────────────────────────────────────────────────────────────────────────
# 1. Build
# ─────────────────────────────────────────────────────────────────────────────

if (-not $SkipBuild) {
    Write-Step "Building Agent"
    dotnet publish $AgentSrc -c Release -o $AgentOut --nologo
    if ($LASTEXITCODE -ne 0) { throw "Agent build failed" }

    Write-Step "Building Dashboard"
    dotnet publish $DashboardSrc -c Release -o $DashboardOut --nologo
    if ($LASTEXITCODE -ne 0) { throw "Dashboard build failed" }

    Write-Step "Building Sensor"
    dotnet publish $SensorSrc -c Release -o $SensorOut --nologo
    if ($LASTEXITCODE -ne 0) { throw "Sensor build failed" }
}

# ─────────────────────────────────────────────────────────────────────────────
# 2. Register Sensor as Windows Service
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Registering Sensor Windows Service"

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($svc) {
    if ($svc.Status -eq "Running") {
        Write-Host "  Stopping running instance..."
        Stop-Service -Name $ServiceName -Force
        $svc.WaitForStatus("Stopped", [TimeSpan]::FromSeconds(15))
    }
    sc.exe config $ServiceName binPath= "`"$SensorExe`""
    if ($LASTEXITCODE -ne 0) { throw "sc.exe config failed" }
    Write-Host "  Updated binary path for existing service '$ServiceName'."
} else {
    sc.exe create $ServiceName binPath= "`"$SensorExe`"" DisplayName= "System Monitor Sensor" start= demand
    if ($LASTEXITCODE -ne 0) { throw "sc.exe create failed" }
    sc.exe description $ServiceName "Collects CPU and memory metrics and forwards them to the System Monitor Agent."
    Write-Host "  Registered new service '$ServiceName'."
}

# ─────────────────────────────────────────────────────────────────────────────
# 3. Start Agent
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Starting Agent  (http://localhost:5000)"
Start-Process -FilePath $AgentExe -WorkingDirectory $AgentOut

Write-Host "  Waiting for Agent to initialise..."
Start-Sleep -Seconds 3

# ─────────────────────────────────────────────────────────────────────────────
# 4. Start Dashboard
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Starting Dashboard  (http://localhost:5069)"
# Blazor WASM needs the ASP.NET Core dev server (DevServer package).
# The published output in $DashboardOut is static files only, with no runnable
# host, so we use dotnet run here. --no-build reuses the Release artifacts that
# dotnet publish already produced above.
Start-Process powershell -ArgumentList @(
    "-NoExit", "-Command",
    "dotnet run --project `"$DashboardSrc`" -c Release --no-build --launch-profile http"
)

# ─────────────────────────────────────────────────────────────────────────────
# 5. Start Sensor
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Starting Sensor  (Windows Service: '$ServiceName')"
Start-Service -Name $ServiceName
Write-Host "  Service started."

Write-Host ""
Write-Host "All services are running." -ForegroundColor Green
Write-Host "  Agent     : http://localhost:5000"
Write-Host "  Dashboard : http://localhost:5069"
Write-Host "  Sensor    : sc query $ServiceName"
