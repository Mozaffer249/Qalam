#!/usr/bin/env bash
# ==============================================================================
# Qalam VPS bootstrap — staging stack
# ==============================================================================
# Runs steps 1–9 of the staging deployment in one shot:
#   - SQL Server 2022 Express install (loopback bind)
#   - sql-bootstrap-qalam.sql  (four DBs + two SQL logins, password Qalam@2026)
#   - Nightly backup cron
#   - Docker engine (if missing)
#   - .env.staging generation with auto-random secrets
#   - docker compose up -d (staging stack)
#   - Nginx + Certbot for api-staging.qalam.net.sa
#
# Idempotent: safe to re-run. Each phase checks before doing.
#
# Run on a fresh Ubuntu 22.04 VPS as root (or with sudo):
#   sudo bash scripts/bootstrap-vps-staging.sh
#
# Prereqs:
#   - Repo already cloned at $REPO_PATH (default /opt/qalam-backend/Qalam)
#   - DNS A record for api-staging.qalam.net.sa already points at this VPS
#   - You have edited the CERTBOT_EMAIL variable below
# ==============================================================================

set -euo pipefail

# ====================== EDIT THESE TWO LINES =================================
CERTBOT_EMAIL="you@example.com"        # ← change to your email before running
REPO_PATH="/opt/qalam-backend/Qalam"   # ← change if your clone is elsewhere
# ============================================================================

DOMAIN_STAGING="api-staging.qalam.net.sa"
SECRETS_FILE="/root/.qalam-staging-secrets"
SA_PASSWORD_FILE="/root/.mssql-sa"
BOOTSTRAP_SQL_PASSWORD="Qalam@2026"  # matches docs/deployment/sql-bootstrap-qalam.sql

# ------------------------------- helpers -------------------------------------
banner() { printf '\n\033[1;36m== %s ==\033[0m\n' "$*"; }
note()   { printf '   • %s\n' "$*"; }
ok()     { printf '\033[1;32m   ✓ %s\033[0m\n' "$*"; }
warn()   { printf '\033[1;33m   ! %s\033[0m\n' "$*"; }
fail()   { printf '\033[1;31m   ✗ %s\033[0m\n' "$*" >&2; exit 1; }

require_root() { [[ $EUID -eq 0 ]] || fail "Run with sudo: sudo bash $0"; }
have_cmd()     { command -v "$1" >/dev/null 2>&1; }

# Generate or reuse a secret from the secrets file
gen_or_read() {
  local key="$1" length="${2:-32}"
  if grep -q "^${key}=" "$SECRETS_FILE" 2>/dev/null; then
    grep "^${key}=" "$SECRETS_FILE" | head -1 | cut -d= -f2-
  else
    local v; v=$(openssl rand -base64 "$length" | tr -d '\n' | tr -d '/+=' | head -c "$length")
    printf '%s=%s\n' "$key" "$v" >> "$SECRETS_FILE"
    printf '%s' "$v"
  fi
}

# ------------------------- phase 0: preflight --------------------------------
banner "Phase 0 — preflight"
require_root

if [[ "$CERTBOT_EMAIL" == "you@example.com" ]]; then
  fail "Edit the CERTBOT_EMAIL variable at the top of this script before running."
fi

[[ -d "$REPO_PATH" ]] || fail "Repo not found at $REPO_PATH. Clone first or edit REPO_PATH at the top."
[[ -f "$REPO_PATH/docker-compose.staging.yml" ]] || fail "$REPO_PATH does not look like a Qalam clone (no docker-compose.staging.yml)."
[[ -f "$REPO_PATH/docs/deployment/sql-bootstrap-qalam.sql" ]] || fail "Run \`git pull\` in $REPO_PATH first — sql-bootstrap-qalam.sql is missing."

. /etc/os-release
[[ "$VERSION_ID" == "22.04" ]] || warn "OS is $PRETTY_NAME, not Ubuntu 22.04. SQL Server install may not work as documented."

mkdir -p "$(dirname "$SECRETS_FILE")"; touch "$SECRETS_FILE"; chmod 600 "$SECRETS_FILE"
ok "preflight passed"

# ------------------------- phase 1: apt prereqs ------------------------------
banner "Phase 1 — apt prereqs"
apt-get update -y
apt-get install -y curl gnupg apt-transport-https ca-certificates ufw
ok "apt prereqs installed"

# ------------------------- phase 2: SQL Server -------------------------------
banner "Phase 2 — install SQL Server 2022 (Express)"
if ! have_cmd /opt/mssql/bin/mssql-conf; then
  curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
    | gpg --dearmor -o /etc/apt/trusted.gpg.d/microsoft.gpg
  curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list \
    -o /etc/apt/sources.list.d/mssql-server-2022.list
  curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/prod.list \
    -o /etc/apt/sources.list.d/msprod.list
  apt-get update -y
  apt-get install -y mssql-server
  ok "mssql-server package installed"
else
  ok "mssql-server already installed"
fi

# Non-interactive setup
if [[ ! -f "$SA_PASSWORD_FILE" ]]; then
  SA_PASSWORD="$(openssl rand -base64 24 | tr -d '/+=' | head -c 24)Qq1"
  printf '%s' "$SA_PASSWORD" > "$SA_PASSWORD_FILE"
  chmod 600 "$SA_PASSWORD_FILE"
  MSSQL_PID=Express ACCEPT_EULA=Y MSSQL_SA_PASSWORD="$SA_PASSWORD" \
    /opt/mssql/bin/mssql-conf -n setup accept-eula
  ok "SQL Server first-time setup complete (SA password saved in $SA_PASSWORD_FILE)"
else
  ok "SA password file already present — skipping mssql-conf setup"
fi

# Bind to 127.0.0.1
/opt/mssql/bin/mssql-conf set network.ipaddress 127.0.0.1 >/dev/null
/opt/mssql/bin/mssql-conf set network.tcpport 1433 >/dev/null
systemctl restart mssql-server
sleep 5

if ss -tlnp | grep -q '127.0.0.1:1433'; then
  ok "mssql-server bound to 127.0.0.1:1433"
else
  fail "mssql-server did not bind to 127.0.0.1:1433 — check 'journalctl -u mssql-server -e'"
fi

# UFW: open SSH + HTTP + HTTPS, keep 1433 closed
ufw allow OpenSSH >/dev/null 2>&1 || true
ufw allow 'Nginx Full' >/dev/null 2>&1 || true
yes | ufw enable >/dev/null 2>&1 || true
ok "UFW: SSH + Nginx Full allowed; 1433 stays closed"

# sqlcmd
if ! have_cmd sqlcmd; then
  ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev
  echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' > /etc/profile.d/mssql-tools.sh
  export PATH="$PATH:/opt/mssql-tools18/bin"
  ok "mssql-tools18 installed"
else
  ok "sqlcmd already installed"
fi
export PATH="$PATH:/opt/mssql-tools18/bin"

# ------------------------- phase 3: bootstrap SQL ----------------------------
banner "Phase 3 — bootstrap databases + logins"
SA_PWD="$(cat "$SA_PASSWORD_FILE")"
sqlcmd -S 127.0.0.1 -U SA -P "$SA_PWD" -C -i "$REPO_PATH/docs/deployment/sql-bootstrap-qalam.sql"

# Smoke test
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P "$BOOTSTRAP_SQL_PASSWORD" -d qalam_staging -C \
  -Q "SELECT DB_NAME();" -h -1 -W | grep -q '^qalam_staging$' \
  && ok "qalam_staging_user → qalam_staging connects" \
  || fail "qalam_staging_user smoke test failed"

sqlcmd -S 127.0.0.1 -U qalam_prod_user -P "$BOOTSTRAP_SQL_PASSWORD" -d qalam_prod -C \
  -Q "SELECT DB_NAME();" -h -1 -W | grep -q '^qalam_prod$' \
  && ok "qalam_prod_user → qalam_prod connects" \
  || fail "qalam_prod_user smoke test failed"

# ------------------------- phase 4: nightly backups --------------------------
banner "Phase 4 — nightly backup cron"
mkdir -p /var/opt/mssql/backups
chown mssql:mssql /var/opt/mssql/backups
chmod 750 /var/opt/mssql/backups

cat > /usr/local/bin/qalam-mssql-backup.sh <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
SA_PWD="$(cat /root/.mssql-sa)"
TS="$(date +%Y%m%d_%H%M%S)"
BACKUP_DIR=/var/opt/mssql/backups
for DB in qalam_staging qalam_messaging_staging qalam_prod qalam_messaging_prod; do
  OUT="${BACKUP_DIR}/${DB}_${TS}.bak"
  /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U SA -P "$SA_PWD" -C \
    -Q "BACKUP DATABASE [${DB}] TO DISK = N'${OUT}' WITH NOFORMAT, NOINIT, NAME = '${DB}-${TS}', SKIP, NOREWIND, NOUNLOAD, COMPRESSION, STATS = 10"
  gzip -f "${OUT}"
done
find "${BACKUP_DIR}" -name '*.bak.gz' -mtime +14 -delete
EOF
chmod 700 /usr/local/bin/qalam-mssql-backup.sh

cat > /etc/cron.d/qalam-mssql-backup <<'EOF'
0 2 * * * root /usr/local/bin/qalam-mssql-backup.sh >> /var/log/qalam-mssql-backup.log 2>&1
EOF
chmod 644 /etc/cron.d/qalam-mssql-backup

# Run once now so we know it works
/usr/local/bin/qalam-mssql-backup.sh
ls /var/opt/mssql/backups/qalam_staging_*.bak.gz >/dev/null && ok "backup script verified (one full run completed)"

# ------------------------- phase 5: docker engine ----------------------------
banner "Phase 5 — Docker"
if ! have_cmd docker; then
  curl -fsSL https://get.docker.com | sh
  ok "Docker installed"
else
  ok "Docker already installed ($(docker --version))"
fi
systemctl enable --now docker

# ------------------------- phase 6: .env.staging -----------------------------
banner "Phase 6 — generate .env.staging"
cd "$REPO_PATH"

if [[ -f .env.staging ]]; then
  warn ".env.staging already exists — skipping generation. Edit it manually if needed."
else
  ENCRYPTION_KEY=$(gen_or_read ENCRYPTION_KEY 32)
  JWT_SECRET=$(gen_or_read JWT_SECRET 48)
  RABBITMQ_PASS=$(gen_or_read RABBITMQ_PASS 24)
  DEFAULT_ADMIN_PASSWORD=$(gen_or_read DEFAULT_ADMIN_PASSWORD 16)

  cat > .env.staging <<EOF
# Auto-generated by scripts/bootstrap-vps-staging.sh on $(date -u +%Y-%m-%dT%H:%M:%SZ)
# Secrets are mirrored in $SECRETS_FILE
DB_CONNECTION_STRING=Server=host.docker.internal,1433;Database=qalam_staging;User Id=qalam_staging_user;Password=${BOOTSTRAP_SQL_PASSWORD};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
MESSAGING_DB_CONNECTION_STRING=Server=host.docker.internal,1433;Database=qalam_messaging_staging;User Id=qalam_staging_user;Password=${BOOTSTRAP_SQL_PASSWORD};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
MIGRATE_ON_STARTUP=true
SEED_DEFAULT_ADMIN=true
DEFAULT_ADMIN_EMAIL=admin@staging.qalam.net.sa
DEFAULT_ADMIN_PASSWORD=${DEFAULT_ADMIN_PASSWORD}
ENCRYPTION_KEY=${ENCRYPTION_KEY}
JWT_SECRET=${JWT_SECRET}
JWT_ISSUER=QalamProject-Staging
JWT_AUDIENCE=QalamProjectUsers-Staging
RABBITMQ_USER=qalam
RABBITMQ_PASS=${RABBITMQ_PASS}
CORS_ALLOWED_ORIGINS=https://teacher-staging.qalam.net.sa,https://admin-staging.qalam.net.sa
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_FROM_NAME=Qalam (staging)
EMAIL_FROM_EMAIL=
EMAIL_USERNAME=
EMAIL_PASSWORD=
TWILIO_ACCOUNT_SID=
TWILIO_AUTH_TOKEN=
TWILIO_FROM_NUMBER=
FIREBASE_KEY_PATH=
FIREBASE_PROJECT_ID=
WASABI_ACCESS_KEY=
WASABI_SECRET_KEY=
WASABI_BUCKET=qalam-storage-staging
WASABI_REGION=ap-southeast-1
WASABI_SERVICE_URL=https://s3.ap-southeast-1.wasabisys.com
EOF
  chmod 600 .env.staging
  ok ".env.staging generated (chmod 600)"
  ok "secrets mirrored to $SECRETS_FILE — write them down somewhere safe!"
fi

# ------------------------- phase 7: bring up staging --------------------------
banner "Phase 7 — docker compose up -d (staging)"
cd "$REPO_PATH"
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build

# Wait for the API to become healthy (max 90 s)
for i in {1..18}; do
  if curl -fsS http://127.0.0.1:8081/health >/dev/null 2>&1; then
    ok "staging API responding on 127.0.0.1:8081"
    break
  fi
  sleep 5
  [[ $i -eq 18 ]] && warn "staging API still not responding after 90 s — check 'docker compose -p qalam-staging logs qalam-api'"
done

# ------------------------- phase 8: Nginx + Certbot --------------------------
banner "Phase 8 — Nginx + HTTPS for $DOMAIN_STAGING"
apt-get install -y nginx certbot python3-certbot-nginx

cat > /etc/nginx/sites-available/$DOMAIN_STAGING <<EOF
server {
    listen 80;
    server_name $DOMAIN_STAGING;
    client_max_body_size 25M;

    location / {
        proxy_pass http://127.0.0.1:8081;
        proxy_http_version 1.1;
        proxy_set_header Host              \$host;
        proxy_set_header X-Real-IP         \$remote_addr;
        proxy_set_header X-Forwarded-For   \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 90s;
    }
}
EOF
ln -sf /etc/nginx/sites-available/$DOMAIN_STAGING /etc/nginx/sites-enabled/$DOMAIN_STAGING
nginx -t
systemctl reload nginx

# DNS sanity check
RESOLVED_IP=$(dig +short "$DOMAIN_STAGING" | tail -1)
VPS_IP=$(curl -fsS https://api.ipify.org || true)
if [[ -n "$VPS_IP" && "$RESOLVED_IP" != "$VPS_IP" ]]; then
  warn "DNS for $DOMAIN_STAGING resolves to $RESOLVED_IP but this VPS is $VPS_IP."
  warn "Skipping Certbot — fix the A record first, then run:"
  warn "  certbot --nginx -d $DOMAIN_STAGING --redirect --agree-tos -m $CERTBOT_EMAIL --non-interactive"
else
  certbot --nginx -d "$DOMAIN_STAGING" --redirect --agree-tos -m "$CERTBOT_EMAIL" --non-interactive
  ok "Let's Encrypt cert installed for $DOMAIN_STAGING"
fi

# ------------------------- phase 9: smoke test --------------------------------
banner "Phase 9 — final smoke test"
if curl -fsS "https://$DOMAIN_STAGING/health" >/dev/null 2>&1; then
  ok "https://$DOMAIN_STAGING/health → 200"
else
  warn "HTTPS health check failed. Try: curl -v https://$DOMAIN_STAGING/health"
fi

# ------------------------- done ----------------------------------------------
banner "All done"
cat <<EOF

  Staging API:       https://$DOMAIN_STAGING
  Swagger:           https://$DOMAIN_STAGING/swagger
  Default admin:     admin@staging.qalam.net.sa
  Admin password:    see $SECRETS_FILE (key DEFAULT_ADMIN_PASSWORD)

  SQL Server SA pwd: $SA_PASSWORD_FILE   (chmod 600)
  All generated secrets: $SECRETS_FILE   (chmod 600)

  Next:
    1. Log in once at https://$DOMAIN_STAGING/swagger with the default admin,
       rotate the password via the API, then edit .env.staging and set
       SEED_DEFAULT_ADMIN=false.
    2. When staging is fully validated, follow docs/deployment/04-production-setup.md
       to bring up the prod stack on port 8080.

EOF
