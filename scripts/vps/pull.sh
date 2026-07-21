#!/usr/bin/env bash
# ==============================================================================
# Qalam VPS pull - sync the repo + submodules to the latest committed pointers.
# ==============================================================================
# The counterpart to scripts/dev/push-submodules.ps1: after you push locally,
# run this on the VPS to fast-forward the superproject AND move the admin/teacher
# submodules onto the exact commits the superproject now points at.
#
# It does NOT build or restart containers - it only updates source. To also
# rebuild the changed services, use scripts/vps/deploy.sh instead (that script
# pulls internally, so you don't need both).
#
# Usage (on the VPS):
#   cd /opt/qalam-backend/Qalam
#   bash scripts/vps/pull.sh
#   bash scripts/vps/pull.sh --branch main
#
# Flags:
#   --branch <name>   Branch to pull (default: main).
#   -h | --help       Show this help.
# ==============================================================================

set -euo pipefail

REPO_PATH="${REPO_PATH:-/opt/qalam-backend/Qalam}"
BRANCH="main"

banner() { printf '\n\033[1;36m== %s ==\033[0m\n' "$*"; }
note()   { printf '   • %s\n' "$*"; }
ok()     { printf '\033[1;32m   ✓ %s\033[0m\n' "$*"; }
warn()   { printf '\033[1;33m   ! %s\033[0m\n' "$*"; }
fail()   { printf '\033[1;31m   ✗ %s\033[0m\n' "$*" >&2; exit 1; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --branch) BRANCH="$2"; shift 2 ;;
    -h|--help) grep '^#' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) fail "Unknown option: $1 (use --help)" ;;
  esac
done

cd "$REPO_PATH" || fail "Repo not found at $REPO_PATH (set REPO_PATH=...)"
[[ -d .git ]] || fail "$REPO_PATH is not a git repo"

banner "Pull superproject ($BRANCH)"
BEFORE="$(git rev-parse HEAD)"
git fetch --prune origin
# --ff-only refuses to create a merge commit: if it fails, the VPS has local
# commits/divergence that must be resolved by hand (we never force here).
git pull --ff-only origin "$BRANCH"
AFTER="$(git rev-parse HEAD)"

if [[ "$BEFORE" == "$AFTER" ]]; then
  ok "superproject already up to date ($AFTER)"
else
  note "was $BEFORE"
  note "now $AFTER"
  ok "superproject fast-forwarded"
fi

banner "Sync submodules to recorded commits"
# `sync` picks up any URL changes in .gitmodules; `update --init --recursive`
# checks each submodule out at the exact commit the superproject points to.
git submodule sync --recursive
git submodule update --init --recursive --force
ok "submodules match superproject pointers"

banner "Current state"
git submodule status --recursive | sed 's/^/   /'
echo
git -C "$REPO_PATH" log -1 --pretty='   HEAD %h  %s  (%an, %ar)'
echo

banner "Done"
note "To rebuild only the changed services: sudo bash scripts/vps/deploy.sh --no-pull"
