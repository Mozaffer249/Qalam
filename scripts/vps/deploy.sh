#!/usr/bin/env bash
# ==============================================================================
# Qalam VPS deploy — incremental, space-safe update
# ==============================================================================
# Pulls the latest code + submodules, then rebuilds ONLY the services whose
# source actually changed since the last deploy, and prunes dangling images +
# build cache afterwards so the disk cannot slowly fill up from rebuilds.
#
# Why this exists:
#   `docker compose up -d --build qalam-api messaging-api qalam-teacher qalam-admin`
#   rebuilds ALL four images every time — even when only one submodule moved.
#   Each rebuild leaves behind a dangling old image + build cache, which is how
#   the 59G root disk hit 100%. This script rebuilds the minimum and cleans up.
#
# Usage (on the VPS):
#   cd /opt/qalam-backend/Qalam
#   sudo bash scripts/vps/deploy.sh                 # incremental (default)
#   sudo bash scripts/vps/deploy.sh --all           # force rebuild everything
#   sudo bash scripts/vps/deploy.sh --no-prune      # skip the cleanup step
#   sudo bash scripts/vps/deploy.sh --env-file .env.staging --project qalam-staging
#
# Flags:
#   --all               Rebuild every service regardless of what changed.
#   --no-prune          Do not prune dangling images / build cache afterwards.
#   --no-pull           Skip `git pull` (rebuild from the working tree as-is).
#   --env-file <path>   Compose env file (default: .env).
#   --file <path>       Compose file (default: docker-compose.yml).
#   --project <name>    Compose project name (default: compose file's own name).
#   --branch <name>     Git branch to pull (default: main).
#   -h | --help         Show this help.
# ==============================================================================

set -euo pipefail

# ------------------------------- defaults ------------------------------------
REPO_PATH="${REPO_PATH:-/opt/qalam-backend/Qalam}"
ENV_FILE=".env"
COMPOSE_FILE="docker-compose.yml"
PROJECT=""
BRANCH="main"
FORCE_ALL=0
DO_PRUNE=1
DO_PULL=1

# ------------------------------- helpers -------------------------------------
banner() { printf '\n\033[1;36m== %s ==\033[0m\n' "$*"; }
note()   { printf '   • %s\n' "$*"; }
ok()     { printf '\033[1;32m   ✓ %s\033[0m\n' "$*"; }
warn()   { printf '\033[1;33m   ! %s\033[0m\n' "$*"; }
fail()   { printf '\033[1;31m   ✗ %s\033[0m\n' "$*" >&2; exit 1; }

# ------------------------------- args ----------------------------------------
while [[ $# -gt 0 ]]; do
  case "$1" in
    --all)       FORCE_ALL=1; shift ;;
    --no-prune)  DO_PRUNE=0; shift ;;
    --no-pull)   DO_PULL=0; shift ;;
    --env-file)  ENV_FILE="$2"; shift 2 ;;
    --file)      COMPOSE_FILE="$2"; shift 2 ;;
    --project)   PROJECT="$2"; shift 2 ;;
    --branch)    BRANCH="$2"; shift 2 ;;
    -h|--help)   grep '^#' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *)           fail "Unknown option: $1 (use --help)" ;;
  esac
done

cd "$REPO_PATH" || fail "Repo not found at $REPO_PATH (set REPO_PATH=...)"
[[ -f "$COMPOSE_FILE" ]] || fail "Compose file not found: $COMPOSE_FILE"
[[ -f "$ENV_FILE" ]]     || fail "Env file not found: $ENV_FILE"

# Build the shared compose invocation (project flag only if provided).
COMPOSE=(docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE")
[[ -n "$PROJECT" ]] && COMPOSE=(docker compose -f "$COMPOSE_FILE" -p "$PROJECT" --env-file "$ENV_FILE")

# ------------------------- phase 1: pull -------------------------------------
banner "Phase 1 — sync source"
BEFORE="$(git rev-parse HEAD)"
if [[ "$DO_PULL" -eq 1 ]]; then
  note "git pull --ff-only --recurse-submodules origin $BRANCH"
  git pull --ff-only --recurse-submodules origin "$BRANCH"
  git submodule update --init --recursive
else
  warn "skipping git pull (--no-pull)"
fi
AFTER="$(git rev-parse HEAD)"

if [[ "$BEFORE" == "$AFTER" && "$FORCE_ALL" -eq 0 && "$DO_PULL" -eq 1 ]]; then
  ok "already up to date ($AFTER) — nothing new to build"
else
  note "was $BEFORE"
  note "now $AFTER"
fi

# ------------------------- phase 2: decide what to rebuild -------------------
banner "Phase 2 — detect changed services"

# Map changed paths -> compose service names.
#   backend (.NET) sources -> qalam-api + messaging-api (they share Core/Data/Infra)
#   apps/admin submodule    -> qalam-admin
#   apps/teacher submodule  -> qalam-teacher
declare -a SERVICES=()

add_service() {
  local svc="$1"
  for existing in "${SERVICES[@]:-}"; do [[ "$existing" == "$svc" ]] && return 0; done
  SERVICES+=("$svc")
}

if [[ "$FORCE_ALL" -eq 1 ]]; then
  note "--all: rebuilding every service"
  add_service messaging-api
  add_service qalam-api
  add_service qalam-admin
  add_service qalam-teacher
else
  CHANGED="$(git diff --name-only "$BEFORE" "$AFTER" || true)"
  if [[ -z "$CHANGED" ]]; then
    note "no changed files detected between deploys"
  else
    note "changed paths:"
    echo "$CHANGED" | sed 's/^/       /'
    while IFS= read -r path; do
      [[ -z "$path" ]] && continue
      case "$path" in
        Qalam.*|*.sln)  add_service qalam-api; add_service messaging-api ;;
        apps/admin*)    add_service qalam-admin ;;
        apps/teacher*)  add_service qalam-teacher ;;
        docker-compose.yml)
          # Compose change can affect any service; safest to rebuild all.
          add_service qalam-api; add_service messaging-api
          add_service qalam-admin; add_service qalam-teacher ;;
      esac
    done <<< "$CHANGED"
  fi
fi

# ------------------------- phase 3: build + recreate -------------------------
banner "Phase 3 — build + recreate"
if [[ "${#SERVICES[@]}" -eq 0 ]]; then
  note "no services need rebuilding — ensuring the stack is up (no build)"
  "${COMPOSE[@]}" up -d
  ok "stack running; nothing rebuilt"
else
  note "rebuilding: ${SERVICES[*]}"
  # Build only the changed services (uses layer cache; unchanged layers reused).
  "${COMPOSE[@]}" build "${SERVICES[@]}"
  # `up -d` recreates only containers whose image/config changed; others untouched.
  "${COMPOSE[@]}" up -d
  ok "rebuilt and recreated: ${SERVICES[*]}"
fi

# ------------------------- phase 4: reclaim space ----------------------------
if [[ "$DO_PRUNE" -eq 1 ]]; then
  banner "Phase 4 — reclaim space (dangling images + build cache)"
  # Dangling images = the old versions the rebuild just replaced. Safe: never
  # touches images referenced by a container. `-a` is intentionally NOT used so
  # in-use base images (dotnet/node/rabbitmq/mssql) are kept for the next build.
  docker image prune -f
  docker builder prune -f
  ok "pruned dangling images + build cache"
else
  warn "skipping prune (--no-prune) — dangling images will accumulate"
fi

# ------------------------- done ----------------------------------------------
banner "Deploy complete"
"${COMPOSE[@]}" ps
echo
df -h / | sed 's/^/   /'
