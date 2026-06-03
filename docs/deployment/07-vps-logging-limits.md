# VPS logging limits (prevent disk-full)

A full root disk on the VPS was traced to **`/var/log/syslog.1` (~40 GB)** — mostly Docker container stdout, UFW block lines, and Docker network events forwarded to rsyslog.

Apply these limits **once per VPS** (and after any fresh server setup).

---

## One command (from repo on VPS)

```bash
cd /opt/qalam-backend/Qalam
git pull
sudo bash scripts/vps/setup-logging-limits.sh
df -h /
```

Then recreate stacks so Compose-level `logging:` limits apply:

```bash
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d
```

---

## What the script configures

| Layer | Limit |
|-------|--------|
| **Docker daemon** (`/etc/docker/daemon.json`) | `json-file`, 10 MB × 3 files per container (default for new containers) |
| **Compose** (`docker-compose.*.yml`) | Same per service via `logging:` (see repo) |
| **journald** | Max 200 MB on disk |
| **rsyslog / logrotate** | Rotate at 100 MB, keep 3 compressed files |
| **UFW** | `LOGLEVEL=low` (stops logging every blocked packet at high verbosity) |
| **Emergency** | Deletes `syslog.*` rotated files larger than 100 MB |

---

## Manual cleanup (if disk is already full)

```bash
sudo rm -f /var/log/syslog.1
docker builder prune -af
docker system prune -af
df -h /
```

---

## App file logs (Serilog volumes)

`qalam-api` writes rolling files under the `staging_app_logs` / `app_logs` volume (`Logs/log-*.txt`):

- Max **10 MB** per file, roll on size
- Keep **7** daily files (~70 MB cap per API)

Inspect:

```bash
docker volume inspect qalam-staging_staging_app_logs --format '{{.Mountpoint}}'
sudo ls -lh "$(docker volume inspect qalam-staging_staging_app_logs --format '{{.Mountpoint}}')"
```

---

## Monitoring

Add to monthly ops or after deploy:

```bash
df -h /
sudo du -sh /var/log/syslog* /var/log/journal 2>/dev/null
docker system df
```

If `/` goes above **85%**, run the setup script again and prune Docker.
