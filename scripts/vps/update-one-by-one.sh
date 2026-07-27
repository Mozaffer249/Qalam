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
# ==============================================================================
set -euo pipefail

REPO_PATH="${REPO_PATH:-/opt/qalam-backend/Qalam}"
ENV_FILE="${ENV_FILE:-.env}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
DO_PULL=1

ALL_SERVICES=(messaging-api qalam-api qalam-admin qalam-teacher)

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

if [[ "$DO_PULL" -eq 1 ]]; then
  note "git pull --ff-only --recurse-submodules origin main"
  git pull --ff-only --recurse-submodules origin main
  git submodule update --init --recursive
  ok "source synced"
fi

for svc in "${SERVICES[@]}"; do
  printf '\n\033[1;36m== %s ==\033[0m\n' "$svc"
  note "stop + remove"
  "${COMPOSE[@]}" stop "$svc" || true
  "${COMPOSE[@]}" rm -f "$svc" || true
  note "build + recreate"
  "${COMPOSE[@]}" up -d --build --force-recreate "$svc"
  ok "$svc running"
done

printf '\n'
"${COMPOSE[@]}" ps
ok "done"
