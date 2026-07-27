param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        "all",
        "api",
        "messaging",
        "admin",
        "teacher",
        "manual-all",
        "deploy-api",
        "status",
        "purge-email-queue"
    )]
    [string]$Name
)

$cmds = @{
    all = @"
cd /opt/qalam-backend/Qalam
sudo bash scripts/vps/update-one-by-one.sh
"@
    api = @"
cd /opt/qalam-backend/Qalam
sudo bash scripts/vps/update-one-by-one.sh --no-pull qalam-api
"@
    messaging = @"
cd /opt/qalam-backend/Qalam
sudo bash scripts/vps/update-one-by-one.sh --no-pull messaging-api
"@
    admin = @"
cd /opt/qalam-backend/Qalam
sudo bash scripts/vps/update-one-by-one.sh --no-pull qalam-admin
"@
    teacher = @"
cd /opt/qalam-backend/Qalam
sudo bash scripts/vps/update-one-by-one.sh --no-pull qalam-teacher
"@
    "manual-all" = @"
cd /opt/qalam-backend/Qalam
git pull --ff-only --recurse-submodules origin main
git submodule update --init --recursive

docker compose stop qalam-api && docker compose rm -f qalam-api && docker compose up -d --build --force-recreate qalam-api
docker compose stop messaging-api && docker compose rm -f messaging-api && docker compose up -d --build --force-recreate messaging-api
docker compose stop qalam-admin && docker compose rm -f qalam-admin && docker compose up -d --build --force-recreate qalam-admin
docker compose stop qalam-teacher && docker compose rm -f qalam-teacher && docker compose up -d --build --force-recreate qalam-teacher

docker compose ps
"@
    "deploy-api" = @"
cd /opt/qalam-backend/Qalam
sudo bash scripts/vps/deploy.sh --no-prune --service qalam-api
"@
    status = @"
cd /opt/qalam-backend/Qalam
docker compose ps
docker logs --tail 80 qalam-backend-api
"@
    "purge-email-queue" = @"
# Purge looping email-queue poison messages, then check DLQ depth.
# Run before/after messaging-api redeploy when bad recipients flood logs.
docker exec qalam-rabbitmq rabbitmqctl list_queues name messages
docker exec qalam-rabbitmq rabbitmqctl purge_queue email-queue
docker exec qalam-rabbitmq rabbitmqctl list_queues name messages
"@
}

$text = $cmds[$Name].Trim()
Set-Clipboard -Value $text

Write-Host ""
Write-Host "===== Copied to clipboard — paste on the VPS =====" -ForegroundColor Green
Write-Host $text
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""
