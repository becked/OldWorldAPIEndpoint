# deploy.ps1 - Build and deploy the mod to Old World mods folder (Windows)

$ErrorActionPreference = "Stop"

# Load environment configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$EnvFile = Join-Path $ScriptDir ".env"

if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
            $name = $matches[1].Trim()
            $value = $matches[2].Trim().Trim('"').Trim("'")
            # Expand %APPDATA% and other environment variables
            $value = [System.Environment]::ExpandEnvironmentVariables($value)
            Set-Item -Path "Env:$name" -Value $value
        }
    }
} else {
    Write-Host "Error: .env file not found" -ForegroundColor Red
    Write-Host "Copy .env.example to .env and configure paths for your system"
    exit 1
}

# Validate required variables
if (-not $env:OLDWORLD_PATH -or $env:OLDWORLD_PATH -eq "/path/to/Steam/steamapps/common/Old World") {
    Write-Host "Error: OLDWORLD_PATH not configured in .env" -ForegroundColor Red
    exit 1
}

if (-not $env:OLDWORLD_MODS_PATH) {
    Write-Host "Error: OLDWORLD_MODS_PATH not configured in .env" -ForegroundColor Red
    exit 1
}

$ModDir = Join-Path $env:OLDWORLD_MODS_PATH "OldWorldAPIEndpoint"

Write-Host "=== Building OldWorldAPIEndpoint ===" -ForegroundColor Cyan
$env:OldWorldPath = $env:OLDWORLD_PATH
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Deploying to Old World ===" -ForegroundColor Cyan

# Create mod directory if it doesn't exist
if (-not (Test-Path $ModDir)) {
    New-Item -ItemType Directory -Path $ModDir -Force | Out-Null
}

# Copy mod files
Copy-Item "ModInfo.xml" -Destination $ModDir -Force
Copy-Item "bin\OldWorldAPIEndpoint.dll" -Destination $ModDir -Force

# Copy Newtonsoft.Json.dll (required dependency)
$NugetPackages = Join-Path $env:USERPROFILE ".nuget\packages\newtonsoft.json\13.0.3"
$NewtonsoftDll = Get-ChildItem -Path $NugetPackages -Recurse -Filter "Newtonsoft.Json.dll" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "net45" } |
    Select-Object -First 1

if ($NewtonsoftDll) {
    Copy-Item $NewtonsoftDll.FullName -Destination $ModDir -Force
    Write-Host "Copied Newtonsoft.Json.dll"
} else {
    Write-Host "WARNING: Newtonsoft.Json.dll not found!" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Deployed successfully ===" -ForegroundColor Green
Get-ChildItem $ModDir | Format-Table Name, Length, LastWriteTime

Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Launch Old World"
Write-Host "2. Enable 'Old World API Endpoint' in Mod Manager"
Write-Host "3. Start or load a game"
Write-Host "4. In PowerShell: Test-NetConnection localhost -Port 9876"
Write-Host "   Or use a TCP client to connect to localhost:9876"
Write-Host "5. End a turn and watch for JSON output"
