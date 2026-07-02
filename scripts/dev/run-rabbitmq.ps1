$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $root
docker compose up -d rabbitmq
