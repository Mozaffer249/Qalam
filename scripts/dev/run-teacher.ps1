$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
. "$PSScriptRoot/load-dotenv.ps1" -EnvFile (Join-Path $root ".env")
. "$PSScriptRoot/resolve-api-dev-url.ps1"

$apiUrl = Resolve-DevApiUrl
$env:VITE_API_URL = $apiUrl

Set-Location (Join-Path $root "apps/teacher")
Write-Host "Teacher -> http://localhost:3000 (API: $apiUrl)" -ForegroundColor Cyan
npm run dev
