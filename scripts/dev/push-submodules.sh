#!/usr/bin/env bash
# =============================================================================
# push-submodules.sh - commit + push the admin/teacher submodules, then bump
# the pointer in the main Qalam repo. One command for the whole flow.
#
# Bash equivalent of push-submodules.ps1 for macOS / Linux.
#
# Usage:
#   ./scripts/dev/push-submodules.sh "your commit message"
#   ./scripts/dev/push-submodules.sh "fix teacher form" -b main
#   ./scripts/dev/push-submodules.sh "wip" --skip-super
# =============================================================================
set -euo pipefail

BRANCH="main"
SKIP_SUPER=false
MESSAGE=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    -b|--branch)  BRANCH="$2"; shift 2 ;;
    --skip-super) SKIP_SUPER=true; shift ;;
    -*)           echo "Unknown flag: $1"; exit 1 ;;
    *)            MESSAGE="$1"; shift ;;
  esac
done

if [[ -z "$MESSAGE" ]]; then
  echo "Usage: $0 \"commit message\" [-b branch] [--skip-super]"
  exit 1
fi

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SUBMODULES=("apps/admin" "apps/teacher")

info()  { printf "\033[36m-> %s\033[0m\n" "$1"; }
ok()    { printf "\033[32m   %s\033[0m\n" "$1"; }
warn()  { printf "\033[33m   %s\033[0m\n" "$1"; }

for sub in "${SUBMODULES[@]}"; do
  path="$ROOT/$sub"

  if [[ ! -e "$path/.git" ]]; then
    warn "$sub not initialized (run: git submodule update --init --recursive) - skipping"
    continue
  fi

  info "Submodule $sub"

  git -C "$path" add -A
  if ! git -C "$path" diff --cached --quiet; then
    current="$(git -C "$path" rev-parse --abbrev-ref HEAD)"
    if [[ "$current" == "HEAD" ]]; then
      git -C "$path" checkout -B "$BRANCH"
      ok "was detached; moved onto $BRANCH"
    fi
    git -C "$path" commit -m "$MESSAGE"
    ok "committed changes"
  else
    ok "no local changes"
  fi

  git -C "$path" push origin "HEAD:$BRANCH"
  ok "pushed to origin/$BRANCH"
done

if $SKIP_SUPER; then
  warn "Skipping superproject pointer update (--skip-super)."
  exit 0
fi

info "Main repo (superproject)"
git -C "$ROOT" add -- "${SUBMODULES[@]}"
if ! git -C "$ROOT" diff --cached --quiet; then
  git -C "$ROOT" commit -m "$MESSAGE"
  ok "committed submodule pointer bump"
else
  ok "no new pointer changes to stage (already committed?)"
fi

git -C "$ROOT" push origin "HEAD:$BRANCH"
ok "pushed superproject to origin/$BRANCH"

info "Done. On the VPS run: sudo bash scripts/vps/deploy.sh"
