$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
. "$PSScriptRoot/load-dotenv.ps1" -EnvFile (Join-Path $root ".env")
. "$PSScriptRoot/set-backend-env.ps1"

$port = if ($env:MESSAGING_API_DEV_PORT) { $env:MESSAGING_API_DEV_PORT } else { "62901" }
$env:ASPNETCORE_URLS = "http://localhost:$port"

Set-Location $root
Write-Host "Messaging API → http://localhost:$port" -ForegroundColor Cyan
dotnet run --project Qalam.MessagingApi
