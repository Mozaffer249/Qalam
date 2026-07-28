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
        "purge-email-queue",
        "staging-up",
        "staging-all",
        "staging-rabbitmq",
        "staging-api",
        "staging-messaging",
        "staging-admin",
        "staging-teacher",
        "staging-status",
        "staging-purge-email"
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
docker compose --env-file .env up -d --build
"@
    "deploy-api" = @"
cd /opt/qalam-backend/Qalam
sudo bash scripts/vps/deploy.sh --no-prune --service qalam-api
"@
    status = @"
cd /opt/qalam-backend/Qalam
docker compose --env-file .env ps
docker logs --tail 80 qalam-backend-api
"@
    "purge-email-queue" = @"
docker exec qalam-rabbitmq rabbitmqctl list_queues name messages
docker exec qalam-rabbitmq rabbitmqctl purge_queue email-queue
docker exec qalam-rabbitmq rabbitmqctl list_queues name messages
"@
    "staging-up" = @"
cd /opt/qalam-backend/Qalam
# Prefer one-by-one (staging-all) — full up --build is slow on VPS
sudo ENV_FILE=.env.staging COMPOSE_FILE=docker-compose.staging.yml COMPOSE_PROJECT_NAME=qalam-staging bash scripts/vps/update-one-by-one.sh --no-pull
"@
    "staging-all" = @"
cd /opt/qalam-backend/Qalam
sudo ENV_FILE=.env.staging COMPOSE_FILE=docker-compose.staging.yml COMPOSE_PROJECT_NAME=qalam-staging bash scripts/vps/update-one-by-one.sh
"@
    "staging-rabbitmq" = @"
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d rabbitmq
"@
    "staging-api" = @"
cd /opt/qalam-backend/Qalam
sudo ENV_FILE=.env.staging COMPOSE_FILE=docker-compose.staging.yml COMPOSE_PROJECT_NAME=qalam-staging bash scripts/vps/update-one-by-one.sh --no-pull qalam-api
"@
    "staging-messaging" = @"
cd /opt/qalam-backend/Qalam
sudo ENV_FILE=.env.staging COMPOSE_FILE=docker-compose.staging.yml COMPOSE_PROJECT_NAME=qalam-staging bash scripts/vps/update-one-by-one.sh --no-pull messaging-api
"@
    "staging-admin" = @"
cd /opt/qalam-backend/Qalam
sudo ENV_FILE=.env.staging COMPOSE_FILE=docker-compose.staging.yml COMPOSE_PROJECT_NAME=qalam-staging bash scripts/vps/update-one-by-one.sh --no-pull qalam-admin
"@
    "staging-teacher" = @"
cd /opt/qalam-backend/Qalam
sudo ENV_FILE=.env.staging COMPOSE_FILE=docker-compose.staging.yml COMPOSE_PROJECT_NAME=qalam-staging bash scripts/vps/update-one-by-one.sh --no-pull qalam-teacher
"@
    "staging-status" = @"
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging ps
docker logs --tail 80 qalam-staging-backend-api
"@
    "staging-purge-email" = @"
docker exec qalam-staging-rabbitmq rabbitmqctl list_queues name messages
docker exec qalam-staging-rabbitmq rabbitmqctl purge_queue email-queue
docker exec qalam-staging-rabbitmq rabbitmqctl list_queues name messages
"@
}

$text = $cmds[$Name].Trim()
Set-Clipboard -Value $text

Write-Host ""
Write-Host "===== Copied to clipboard — paste on the VPS =====" -ForegroundColor Green
Write-Host $text
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""
