#!/usr/bin/env bash
# E2E smoke test: manual teacher activation flow (admin steps on an existing pending teacher).
#
# Usage:
#   export BASE_URL=http://127.0.0.1:8080
#   export ADMIN_EMAIL=admin@staging.qalam.net.sa
#   export ADMIN_PASSWORD='your-password'
#   export TEACHER_ID=12          # pending teacher with docs + subjects ready
#   ./scripts/test-teacher-activation-e2e.sh
#
# Optional (negative cases):
#   SKIP_ACTIVATE=1               # only assert canBeActivated gate, do not POST Activate
#
# Teacher registration (OTP 1234 in Development/Staging):
#   See docs/Teacher-Registration-Flow.md and Postman/Qalam-Testing.postman_environment.json

set -euo pipefail

BASE_URL="${BASE_URL:-http://127.0.0.1:8080}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin@123456}"
TEACHER_ID="${TEACHER_ID:-}"

red() { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }
info() { printf '→ %s\n' "$*"; }

json_get() {
  local expr="$1"
  python3 -c "import json,sys; d=json.load(sys.stdin); print($expr)" 2>/dev/null
}

require_jq_or_python() {
  command -v python3 >/dev/null || { red "python3 required"; exit 1; }
}

api_health() {
  local code
  code=$(curl -sS -o /dev/null -w '%{http_code}' "${BASE_URL}/health" || true)
  [[ "$code" == "200" ]] || { red "API not reachable at ${BASE_URL}/health (HTTP ${code})"; exit 1; }
  green "API healthy: ${BASE_URL}"
}

admin_login() {
  info "Admin login"
  local resp
  resp=$(curl -sS -X POST "${BASE_URL}/Api/V1/Authentication/Login" \
    -H 'Content-Type: application/json' \
    -d "{\"email\":\"${ADMIN_EMAIL}\",\"password\":\"${ADMIN_PASSWORD}\"}")
  ADMIN_TOKEN=$(echo "$resp" | json_get "d.get('data',{}).get('accessToken') or d.get('Data',{}).get('accessToken','')")
  [[ -n "$ADMIN_TOKEN" ]] || { red "Admin login failed: $resp"; exit 1; }
  green "Admin token acquired"
}

resolve_teacher_id() {
  if [[ -n "$TEACHER_ID" ]]; then
    info "Using TEACHER_ID=${TEACHER_ID}"
    return
  fi
  info "Fetching first pending teacher"
  local resp
  resp=$(curl -sS "${BASE_URL}/Api/V1/Admin/TeacherManagement/Pending?pageNumber=1&pageSize=1" \
    -H "Authorization: Bearer ${ADMIN_TOKEN}")
  TEACHER_ID=$(echo "$resp" | json_get "d.get('data',d.get('Data',{})).get('items',[{}])[0].get('teacherId') or d.get('data',d.get('Data',{})).get('items',[{}])[0].get('id','')")
  [[ -n "$TEACHER_ID" && "$TEACHER_ID" != "None" ]] || { red "No pending teacher found. Set TEACHER_ID=..."; exit 1; }
  green "Teacher ID: ${TEACHER_ID}"
}

teacher_details() {
  curl -sS "${BASE_URL}/Api/V1/Admin/TeacherManagement/${TEACHER_ID}" \
    -H "Authorization: Bearer ${ADMIN_TOKEN}"
}

approve_pending_docs() {
  local details="$1"
  local doc_ids
  doc_ids=$(echo "$details" | python3 -c "
import json,sys
d=json.load(sys.stdin)
data=d.get('data') or d.get('Data') or {}
docs=data.get('documents') or data.get('Documents') or []
for doc in docs:
    status=doc.get('verificationStatus') or doc.get('VerificationStatus')
    if status in (1, 'Pending', 'pending'):
        print(doc.get('id') or doc.get('Id'))
")
  for doc_id in $doc_ids; do
    info "Approve document ${doc_id}"
    curl -sS -X POST "${BASE_URL}/Api/V1/Admin/TeacherManagement/${TEACHER_ID}/Documents/${doc_id}/Approve" \
      -H "Authorization: Bearer ${ADMIN_TOKEN}" >/dev/null
  done
}

approve_pending_subjects() {
  local details="$1"
  local sub_ids
  sub_ids=$(echo "$details" | python3 -c "
import json,sys
d=json.load(sys.stdin)
data=d.get('data') or d.get('Data') or {}
subs=data.get('subjects') or data.get('Subjects') or []
for s in subs:
    status=s.get('verificationStatus') or s.get('VerificationStatus')
    if status in (1, 'Pending', 'pending'):
        print(s.get('id') or s.get('Id'))
")
  for sub_id in $sub_ids; do
    info "Approve subject ${sub_id}"
    curl -sS -X POST "${BASE_URL}/Api/V1/Admin/TeacherManagement/${TEACHER_ID}/Subjects/${sub_id}/Approve" \
      -H "Authorization: Bearer ${ADMIN_TOKEN}" >/dev/null
  done
}

assert_can_activate() {
  local details="$1"
  local can status
  can=$(echo "$details" | json_get "str((d.get('data') or d.get('Data') or {}).get('canBeActivated') or (d.get('data') or d.get('Data') or {}).get('CanBeActivated', False)).lower()")
  status=$(echo "$details" | json_get "(d.get('data') or d.get('Data') or {}).get('status') or (d.get('data') or d.get('Data') or {}).get('Status','')")
  info "status=${status} canBeActivated=${can}"
  [[ "$can" == "true" ]] || { red "Expected canBeActivated=true before Activate"; echo "$details" | python3 -m json.tool; exit 1; }
  [[ "$status" != "Active" && "$status" != "2" ]] || { red "Teacher already Active before Activate call"; exit 1; }
  green "Gate OK: canBeActivated=true, not yet Active"
}

post_activate() {
  info "POST Activate teacher ${TEACHER_ID}"
  local resp code
  resp=$(curl -sS -w '\n%{http_code}' -X POST \
    "${BASE_URL}/Api/V1/Admin/TeacherManagement/${TEACHER_ID}/Activate" \
    -H "Authorization: Bearer ${ADMIN_TOKEN}")
  code=$(echo "$resp" | tail -1)
  body=$(echo "$resp" | sed '$d')
  [[ "$code" == "200" ]] || { red "Activate failed HTTP ${code}: ${body}"; exit 1; }
  green "Activate succeeded"
}

assert_active() {
  local details
  details=$(teacher_details)
  local status
  status=$(echo "$details" | json_get "(d.get('data') or d.get('Data') or {}).get('status') or (d.get('data') or d.get('Data') or {}).get('Status','')")
  [[ "$status" == "Active" || "$status" == "2" ]] || { red "Expected Active after Activate, got ${status}"; exit 1; }
  green "Teacher is Active"
}

main() {
  require_jq_or_python
  api_health
  admin_login
  resolve_teacher_id

  local details
  details=$(teacher_details)
  approve_pending_docs "$details"
  approve_pending_subjects "$details"

  details=$(teacher_details)
  assert_can_activate "$details"

  if [[ "${SKIP_ACTIVATE:-0}" == "1" ]]; then
    info "SKIP_ACTIVATE=1 — stopping before POST Activate"
    exit 0
  fi

  post_activate
  assert_active
  green "E2E activation flow passed"
}

main "$@"
