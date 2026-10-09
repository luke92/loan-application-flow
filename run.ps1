# Runs the backend API, the mock external service, and the frontend together,
# locally (no Docker) on Windows. Each service opens in its own PowerShell
# window; this script waits and closes all of them on Ctrl+C / exit.
#
# Usage:  .\run.ps1
# If scripts are blocked:  powershell -ExecutionPolicy Bypass -File .\run.ps1

$ErrorActionPreference = 'Stop'

$Root     = $PSScriptRoot
$Backend  = Join-Path $Root 'backend'
$Mock     = Join-Path $Root 'mock-external-service'
$Frontend = Join-Path $Root 'frontend'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet SDK not found. Install .NET 10 SDK and try again.' }
if (-not (Get-Command npm -ErrorAction SilentlyContinue))    { throw 'npm not found. Install Node.js 18+ and try again.' }

if (-not (Test-Path (Join-Path $Mock 'node_modules'))) {
    Write-Host '==> Installing mock-external-service dependencies...'
    Push-Location $Mock; npm install; Pop-Location
}

$envLocal = Join-Path $Frontend '.env.local'
$envExample = Join-Path $Frontend '.env.local.example'
if (-not (Test-Path $envLocal) -and (Test-Path $envExample)) {
    Copy-Item $envExample $envLocal
}

if (-not (Test-Path (Join-Path $Frontend 'node_modules'))) {
    Write-Host '==> Installing frontend dependencies...'
    Push-Location $Frontend; npm install; Pop-Location
}

Write-Host ''
Write-Host '==> Backend API      http://localhost:5080'
Write-Host '==> Mock service     http://localhost:4000'
Write-Host '==> Frontend         http://localhost:3000'
Write-Host 'Press Ctrl+C to stop everything.'
Write-Host ''

function Start-Service($title, $dir, $command) {
    $cmd = "`$Host.UI.RawUI.WindowTitle = '$title'; Set-Location '$dir'; $command"
    Start-Process powershell -ArgumentList '-NoExit', '-Command', $cmd -PassThru
}

$procs = @(
    (Start-Service 'backend'  $Backend  '$env:ASPNETCORE_ENVIRONMENT = "Development"; dotnet run --project src/Api --urls http://localhost:5080'),
    (Start-Service 'mock'     $Mock     'npm start'),
    (Start-Service 'frontend' $Frontend 'npm run dev')
)

try {
    while ($true) { Start-Sleep -Seconds 1 }
}
finally {
    Write-Host 'Stopping all services...'
    foreach ($p in $procs) {
        if ($p -and -not $p.HasExited) { & taskkill /PID $p.Id /T /F | Out-Null }
    }
}
