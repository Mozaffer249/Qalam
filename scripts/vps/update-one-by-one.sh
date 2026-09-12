#!/usr/bin/env bash
# ==============================================================================
# Update Qalam compose services one by one (stop → remove → build/recreate).
# Run on the VPS from the repo root.
#
# Usage:
#   cd /opt/qalam-backend/Qalam
#   sudo bash scripts/vps/update-one-by-one.sh              # all app services
#   sudo bash scripts/vps/update-one-by-one.sh qalam-api     # one service
#   sudo bash scripts/vps/update-one-by-one.sh qalam-api qalam-admin
#   sudo bash scripts/vps/update-one-by-one.sh --no-pull ...
#
# Email poison / messaging-api redeploy:
#   If email-queue is looping bad messages (e.g. invalid recipient), purge before or after
#   redeploying messaging-api, then inspect permanent failures in email-queue.dlq:
#     docker exec qalam-rabbitmq rabbitmqctl list_queues name messages
#     docker exec qalam-rabbitmq rabbitmqctl purge_queue email-queue
#     docker exec qalam-rabbitmq rabbitmqctl list_queues name messages | grep email
# ==============================================================================
set -euo pipefail

REPO_PATH="${REPO_PATH:-/opt/qalam-backend/Qalam}"
ENV_FILE="${ENV_FILE:-.env}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
# Optional: e.g. COMPOSE_PROJECT_NAME=qalam-staging (staging file also sets name: qalam-staging)
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-}"
DO_PULL=1

# rabbitmq first (start only, never recreate), then apps that depend on it
ALL_SERVICES=(rabbitmq messaging-api qalam-api qalam-admin qalam-teacher)

fail() { printf '\033[1;31m✗ %s\033[0m\n' "$*" >&2; exit 1; }
ok()   { printf '\033[1;32m✓ %s\033[0m\n' "$*"; }
note() { printf '  • %s\n' "$*"; }

SERVICES=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-pull) DO_PULL=0; shift ;;
    -h|--help)
      sed -n '2,14p' "$0" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    *) SERVICES+=("$1"); shift ;;
  esac
done

[[ ${#SERVICES[@]} -eq 0 ]] && SERVICES=("${ALL_SERVICES[@]}")

cd "$REPO_PATH" || fail "Repo not found: $REPO_PATH"
[[ -f "$COMPOSE_FILE" ]] || fail "Missing $COMPOSE_FILE"
[[ -f "$ENV_FILE" ]] || fail "Missing $ENV_FILE"

COMPOSE=(docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE")
if [[ -n "$COMPOSE_PROJECT_NAME" ]]; then
  COMPOSE+=(-p "$COMPOSE_PROJECT_NAME")
fi

if [[ "$DO_PULL" -eq 1 ]]; then
  note "git pull --ff-only --recurse-submodules origin main"
  git pull --ff-only --recurse-submodules origin main
  git submodule update --init --recursive
  ok "source synced"
fi

needs_rabbitmq() {
  case "$1" in
    messaging-api|qalam-api) return 0 ;;
    *) return 1 ;;
  esac
}

# Never recreate rabbitmq when deploying apps:
# - `up --force-recreate messaging-api` without --no-deps also recreates rabbitmq
#   (depends_on) and waits on its healthcheck (~60s+): "Container qalam-rabbitmq Error"
# - `up -d rabbitmq` still recreates if compose spec changed (hostname / healthcheck).
#   Recreating with a new hostname against rabbitmq_data breaks Erlang node name.
ensure_rabbitmq() {
  note "ensure rabbitmq is up (never recreate)"
  local cid
  cid="$("${COMPOSE[@]}" ps -aq rabbitmq 2>/dev/null || true)"
  if [[ -n "${cid}" ]]; then
    if [[ "$(docker inspect -f '{{.State.Running}}' "${cid}" 2>/dev/null || true)" == "true" ]]; then
      note "rabbitmq already running — leave it"
      return 0
    fi
    note "rabbitmq exists but is stopped — docker start (no recreate)"
    docker start "${cid}" >/dev/null
    return 0
  fi
  "${COMPOSE[@]}" up -d --no-recreate rabbitmq
}

for svc in "${SERVICES[@]}"; do
  printf '\n\033[1;36m== %s ==\033[0m\n' "$svc"
  if [[ "$svc" == "rabbitmq" ]]; then
    ensure_rabbitmq
  else
    note "stop + remove"
    "${COMPOSE[@]}" stop "$svc" || true
    "${COMPOSE[@]}" rm -f "$svc" || true
    if needs_rabbitmq "$svc"; then
      ensure_rabbitmq
    fi
    note "build + recreate (--no-deps, leave rabbitmq alone)"
    "${COMPOSE[@]}" up -d --no-deps --build --force-recreate "$svc"
  fi
  ok "$svc running"
done

printf '\n'
"${COMPOSE[@]}" ps
ok "done"
