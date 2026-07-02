$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
. "$PSScriptRoot/load-dotenv.ps1" -EnvFile (Join-Path $root ".env")

$apiUrl = if ($env:API_DEV_URL) { $env:API_DEV_URL } else { "http://localhost:62900" }
$port = if ($env:ADMIN_DEV_PORT) { $env:ADMIN_DEV_PORT } else { "3005" }

$env:NEXT_PUBLIC_API_URL = $apiUrl
$env:NEXT_PUBLIC_ENV = "dev"

Set-Location (Join-Path $root "apps/admin")
Write-Host "Admin → http://localhost:$port (API: $apiUrl)" -ForegroundColor Cyan
npm run dev
