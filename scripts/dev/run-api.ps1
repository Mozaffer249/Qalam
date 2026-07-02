$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
. "$PSScriptRoot/load-dotenv.ps1" -EnvFile (Join-Path $root ".env")
. "$PSScriptRoot/set-backend-env.ps1"

$port = if ($env:API_DEV_PORT) { $env:API_DEV_PORT } else { "62900" }
$env:ASPNETCORE_URLS = "http://localhost:$port"

Set-Location $root
Write-Host "Qalam API → http://localhost:$port" -ForegroundColor Cyan
Write-Host "Swagger  → http://localhost:$port/swagger" -ForegroundColor Cyan
dotnet run --project Qalam.Api
