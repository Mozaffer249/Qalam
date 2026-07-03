$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
. "$PSScriptRoot/load-dotenv.ps1" -EnvFile (Join-Path $root ".env")
. "$PSScriptRoot/resolve-api-dev-url.ps1"

$apiUrl = Resolve-DevApiUrl
$port = if ($env:ADMIN_DEV_PORT) { $env:ADMIN_DEV_PORT } else { "3005" }

$env:NEXT_PUBLIC_API_URL = $apiUrl
$env:NEXT_PUBLIC_ENV = "dev"

Set-Location (Join-Path $root "apps/admin")
Write-Host "Admin -> http://localhost:$port (API: $apiUrl)" -ForegroundColor Cyan
npm run dev
