$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
. "$PSScriptRoot/load-dotenv.ps1" -EnvFile (Join-Path $root ".env")

$apiUrl = if ($env:API_DEV_URL) { $env:API_DEV_URL } else { "http://localhost:62900" }
$env:VITE_API_URL = $apiUrl

Set-Location (Join-Path $root "apps/teacher")
Write-Host "Teacher → http://localhost:3000 (API: $apiUrl)" -ForegroundColor Cyan
npm run dev
