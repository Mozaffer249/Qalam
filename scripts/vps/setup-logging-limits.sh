#!/usr/bin/env bash
# Caps host + Docker logging so /var/log/syslog cannot fill the root disk again.
# Run once on the VPS as root after git pull:
#   sudo bash scripts/vps/setup-logging-limits.sh
set -euo pipefail

if [[ "${EUID:-$(id -u)}" -ne 0 ]]; then
  echo "Run as root: sudo bash $0"
  exit 1
fi

echo "==> Removing oversized rotated syslog files (>100MB)..."
find /var/log -maxdepth 1 -name 'syslog.*' -size +100M -print -delete 2>/dev/null || true

echo "==> journald size cap (200M)..."
mkdir -p /etc/systemd/journald.conf.d
install -m 644 /dev/stdin /etc/systemd/journald.conf.d/qalam-size-limit.conf <<'EOF'
[Journal]
SystemMaxUse=200M
RuntimeMaxUse=100M
MaxRetentionSec=7day
EOF
systemctl restart systemd-journald
journalctl --vacuum-size=200M >/dev/null || true

echo "==> rsyslog logrotate (max 100M per file, keep 3 rotations)..."
if [[ -f /etc/logrotate.d/rsyslog ]]; then
  cp -a /etc/logrotate.d/rsyslog "/etc/logrotate.d/rsyslog.bak.$(date +%Y%m%d)"
  if ! grep -q 'maxsize' /etc/logrotate.d/rsyslog; then
    sed -i '/^\/var\/log\/syslog/,/^\}/ {
      /^}/ i\    maxsize 100M
    }' /etc/logrotate.d/rsyslog
  fi
  if ! grep -q '^[[:space:]]*rotate 3' /etc/logrotate.d/rsyslog; then
    sed -i 's/^[[:space:]]*rotate [0-9]*/    rotate 3/' /etc/logrotate.d/rsyslog
  fi
  logrotate -f /etc/logrotate.d/rsyslog 2>/dev/null || true
else
  install -m 644 /dev/stdin /etc/logrotate.d/rsyslog <<'EOF'
/var/log/syslog
/var/log/mail.log
/var/log/mail.info
/var/log/mail.warn
/var/log/mail.err
/var/log/daemon.log
/var/log/kern.log
/var/log/auth.log
/var/log/user.log
/var/log/lpr.log
/var/log/cron.log
/var/log/debug
/var/log/messages
{
    rotate 3
    daily
    missingok
    notifempty
    compress
    delaycompress
    maxsize 100M
    sharedscripts
    postrotate
        /usr/lib/rsyslog/rsyslog-rotate
    endscript
}
EOF
fi

echo "==> UFW log level low (fewer [UFW BLOCK] lines in syslog)..."
if command -v ufw >/dev/null 2>&1; then
  ufw logging low || true
  if [[ -f /etc/ufw/ufw.conf ]]; then
    sed -i 's/^LOGLEVEL=.*/LOGLEVEL=low/' /etc/ufw/ufw.conf
  fi
fi

echo "==> Docker daemon default log rotation (10m x 3 files per container)..."
mkdir -p /etc/docker
python3 <<'PY'
import json
from pathlib import Path

path = Path("/etc/docker/daemon.json")
cfg = {}
if path.exists() and path.read_text().strip():
    cfg = json.loads(path.read_text())

cfg["log-driver"] = "json-file"
cfg.setdefault("log-opts", {})
cfg["log-opts"]["max-size"] = "10m"
cfg["log-opts"]["max-file"] = "3"

path.write_text(json.dumps(cfg, indent=2) + "\n")
PY

if systemctl is-active --quiet docker; then
  systemctl restart docker
  echo "    Docker restarted — recreate stacks: docker compose ... up -d"
else
  echo "    Docker not running; limits apply on next start."
fi

echo ""
echo "Done. Check free space:"
df -h /
echo ""
echo "Optional: recreate staging/prod so compose logging: blocks apply:"
echo "  cd /opt/qalam-backend/Qalam"
echo "  docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d"
