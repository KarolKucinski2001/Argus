# Runs Argus locally: starts Azurite when needed, then builds and runs the https profile.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

function Test-Port([int]$port) {
    $null -ne (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
}

if (-not (Test-Port 10002)) {
    Write-Host 'Starting Azurite (table storage on 127.0.0.1:10002)...'
    $azuriteData = Join-Path $env:TEMP 'azurite'
    Start-Process -FilePath 'npx.cmd' `
        -ArgumentList '--yes', 'azurite', '--silent', '--location', $azuriteData, '--skipApiVersionCheck' `
        -WindowStyle Hidden | Out-Null
    $deadline = (Get-Date).AddSeconds(60)
    while (-not (Test-Port 10002)) {
        if ((Get-Date) -gt $deadline) { throw 'Azurite did not start on port 10002 within 60 seconds.' }
        Start-Sleep -Seconds 1
    }
}

$owners = Get-NetTCPConnection -LocalPort 7219, 5027 -State Listen -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess -Unique
foreach ($ownerId in $owners) {
    $process = Get-CimInstance Win32_Process -Filter "ProcessId = $ownerId"
    if ($null -eq $process) { continue }
    if ($process.CommandLine -notmatch 'bootstrap-scaffold') {
        throw "Port 7219 or 5027 is used by another application (PID $ownerId). Stop it first."
    }
    Write-Host "Stopping previous Argus instance (PID $ownerId)..."
    Stop-Process -Id $ownerId -Force
}
$deadline = (Get-Date).AddSeconds(15)
while ((Test-Port 7219) -or (Test-Port 5027)) {
    if ((Get-Date) -gt $deadline) { throw 'Previous Argus instance did not release its ports.' }
    Start-Sleep -Milliseconds 500
}

# Build first so the browser never runs a stale binary. The app is started through
# `dotnet <dll>` because the generated .exe apphost may be blocked by machine policy.
$project = Join-Path $repoRoot '.bootstrap-scaffold.csproj'
dotnet build $project -c Debug
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

Push-Location $repoRoot
try {
    dotnet (Join-Path $repoRoot 'bin\Debug\net10.0\.bootstrap-scaffold.dll') `
        --environment Development --urls 'https://localhost:7219;http://localhost:5027'
}
finally {
    Pop-Location
}
