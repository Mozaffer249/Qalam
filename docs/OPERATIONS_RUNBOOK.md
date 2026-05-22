# Operations Runbook

Day-2 procedures for the Qalam staging + production stacks running on the VPS.

All commands assume:
- You are the `deploy` user on the VPS (`sudo -iu deploy`), with `sudo` for the SQL Server / Nginx / Certbot bits.
- Compose project names: `qalam-staging` and `qalam-prod`.
- Compose files: `docker-compose.staging.yml` and `docker-compose.prod.yml` in one repo (e.g. `/opt/qalam-backend/Qalam`).

---

## 1. Apply a migration to production (the canonical release flow)

Production has `MIGRATE_ON_STARTUP=false`. Migrations are applied manually, in a window you control.

```bash
# 1. On your laptop / CI: ship the new migration in a tagged release.
#    git push origin v0.2.0

# 2. On the VPS, fetch the tag in staging FIRST and run staging through it.
cd /opt/qalam-backend/Qalam
git fetch --tags && git checkout v0.2.0
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build
# Staging auto-migrates; smoke-test it before touching prod.

# 3. Once staging is green, do the same in prod — but apply migrations explicitly first.
cd /opt/qalam-backend/Qalam
git fetch --tags && git checkout v0.2.0

# Rebuild the image but don't start it yet.
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod build qalam-api

# Run a throwaway container that ONLY applies migrations, with the prod connection string.
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod \
  run --rm \
  -e MIGRATE_ON_STARTUP=true \
  -e SEED_DEFAULT_ADMIN=false \
  qalam-api bash -lc 'echo "Migration run completes when the API logs Database migrations applied successfully then we Ctrl-C"; dotnet Qalam.Api.dll &
                       APP_PID=$!
                       while ! grep -q "Database migrations applied successfully" /app/Logs/log-*.txt 2>/dev/null; do sleep 2; done
                       kill $APP_PID || true'

# 4. Bring the long-lived prod stack back up with the new image.
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d
docker compose -p qalam-prod logs -f qalam-api
# Expect: "Skipping auto-migration in Production (MIGRATE_ON_STARTUP=false)"
```

If the migration fails: the throwaway container exits non-zero, **prod stays on the old image**, and you investigate at leisure.

---

## 2. Nightly backup

Already installed by [`deployment/01-sql-server-install.md`](./deployment/01-sql-server-install.md). It runs at 02:00, dumps `.bak` for each of the four DBs to `/var/opt/mssql/backups`, gzips, and prunes anything older than 14 days.

Check it ran last night:

```bash
ls -lh /var/opt/mssql/backups/*.bak.gz | sort -k9 | tail -8
sudo tail /var/log/qalam-mssql-backup.log
```

Manual run (use when you're about to do something risky):

```bash
sudo /usr/local/bin/qalam-mssql-backup.sh
```

**Offsite copy is your responsibility** — local backups die with the VPS. Install `rclone`, configure a remote (Alibaba OSS / S3 / Backblaze), and add a daily push:

```bash
# Example (after `rclone config` to set up the "offsite" remote)
sudo tee /etc/cron.d/qalam-mssql-offsite > /dev/null <<'EOF'
30 2 * * * root rclone sync /var/opt/mssql/backups offsite:qalam-backups --max-age 2d >> /var/log/qalam-offsite.log 2>&1
EOF
```

---

## 3. Restore from backup (drill or real)

```bash
# 1. Pick a backup
ls /var/opt/mssql/backups/qalam_prod_*.bak.gz | tail -3
sudo gunzip -k /var/opt/mssql/backups/qalam_prod_20260513_020003.bak.gz
# Leaves both the .bak.gz and a usable .bak.

# 2. Restore into a SCRATCH database (never overwrite live prod from this script).
sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C <<'SQL'
RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/backups/qalam_prod_20260513_020003.bak';
GO
-- Note the LogicalName values from the output and use them below.
RESTORE DATABASE [qalam_restore_test]
  FROM DISK = N'/var/opt/mssql/backups/qalam_prod_20260513_020003.bak'
  WITH
    MOVE 'qalam_prod'     TO '/var/opt/mssql/data/qalam_restore_test.mdf',
    MOVE 'qalam_prod_log' TO '/var/opt/mssql/data/qalam_restore_test_log.ldf',
    REPLACE, RECOVERY;
GO

-- Spot-check
SELECT COUNT(*) FROM [qalam_restore_test].dbo.AspNetUsers;
GO

-- Cleanup when done.
DROP DATABASE qalam_restore_test;
GO
SQL
```

**Real disaster restore into prod** is the same with `MOVE ... qalam_prod.mdf` and `WITH REPLACE`. Coordinate downtime, stop the API stack first:

```bash
docker compose -p qalam-prod down
# Restore via the SQL block above using qalam_prod / qalam_messaging_prod targets.
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d
```

---

## 4. Rotate the JWT secret

This invalidates **every** outstanding access + refresh token immediately. Users will be forced to re-login. Communicate accordingly.

```bash
cd /opt/qalam-backend/Qalam
NEW_SECRET="$(openssl rand -base64 48)"
sed -i "s|^JWT_SECRET=.*|JWT_SECRET=${NEW_SECRET}|" .env.prod

docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --force-recreate qalam-api
# Old tokens → 401 once the container is up.
```

Repeat the same for staging when needed (`.env.staging` in the same repo).

---

## 5. Rotate the SQL Server login password

Change the password in SQL, then update the env file and restart the API + messaging containers (RabbitMQ doesn't touch SQL).

```bash
# 1. Change the password in SQL
NEW_PWD="$(openssl rand -base64 24)"
sudo bash -c "sqlcmd -S 127.0.0.1 -U SA -P \"\$(cat /root/.mssql-sa)\" -C -Q \"ALTER LOGIN qalam_prod_user WITH PASSWORD = '${NEW_PWD}'\""

# 2. Update the env file (both connection strings use the same login).
cd /opt/qalam-backend/Qalam
sed -i "s|Password=[^;]*;Encrypt=True|Password=${NEW_PWD};Encrypt=True|g" .env.prod
# Sanity-check the substitution happened.
grep -c "Password=${NEW_PWD}" .env.prod   # expect: 2

# 3. Recreate API + messaging containers (RabbitMQ doesn't need it).
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --force-recreate qalam-api messaging-api
```

---

## 6. Renew TLS certs

Certbot installs a systemd timer (`systemctl list-timers | grep certbot`) that runs twice daily and renews anything in the < 30-day window. Confirm it works:

```bash
sudo certbot renew --dry-run
```

Force a renewal manually if needed:

```bash
sudo certbot renew --force-renewal -d api-staging.qalam.net.sa
sudo systemctl reload nginx
```

---

## 7. Promote a staging build to production

The build is a git tag; the data is not promoted.

```bash
TAG=v0.2.0

# Staging — already on the tag from your release flow.
cd /opt/qalam-backend/Qalam
git log -1 --decorate     # confirm HEAD is at the tag

# Production
cd /opt/qalam-backend/Qalam
git fetch --tags
git checkout "$TAG"
# Run the migration procedure from §1, then up -d.
```

**Do not copy staging data into prod.** Promote code, not data.

---

## 8. Common log lookups

```bash
# All recent prod logs (last 1000 lines)
docker compose -p qalam-prod logs --tail=1000 qalam-api

# Follow live
docker compose -p qalam-prod logs -f qalam-api

# Search for migration history at boot
docker compose -p qalam-prod logs qalam-api | grep -E "migration|Database"

# Verify the enrollment expiration background service is alive
docker compose -p qalam-prod logs qalam-api | grep EnrollmentExpirationService

# Persistent file logs (mounted volume on the host)
docker volume inspect qalam-prod_app_logs --format '{{.Mountpoint}}'
sudo tail -F "$(docker volume inspect qalam-prod_app_logs --format '{{.Mountpoint}}')/log-$(date +%Y%m%d).txt"
```

---

## 9. Stack lifecycle commands

| Action | Staging | Production |
|--------|---------|------------|
| Bring up | `cd /opt/qalam-backend/Qalam && docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build` | `cd /opt/qalam-backend/Qalam && docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d` |
| Restart API only | `docker compose -p qalam-staging restart qalam-api` | `docker compose -p qalam-prod restart qalam-api` |
| Tail logs | `docker compose -p qalam-staging logs -f qalam-api` | `docker compose -p qalam-prod logs -f qalam-api` |
| Stop (keep data) | `docker compose -p qalam-staging stop` | `docker compose -p qalam-prod stop` |
| Bring down (keep volumes) | `docker compose -p qalam-staging down` | `docker compose -p qalam-prod down` |
| Delete EVERYTHING incl. data | `docker compose -p qalam-staging down -v` | **NEVER** without an offsite backup |

---

## 10. Common failure modes

| Symptom | First thing to check |
|---------|----------------------|
| API container restarting in a loop | `docker compose -p qalam-prod logs qalam-api` — usually bad connection string or SQL Server down |
| 502 Bad Gateway from Nginx | Is the API container up? `curl http://127.0.0.1:8080/health` from the VPS host |
| 401 on every request right after a deploy | JWT secret changed; expected for in-flight tokens. If unexpected, check `.env.prod` for an accidental edit |
| `Database 'qalam_prod' is not accessible` | SQL login password drifted between SQL and `.env.prod`. Run §5 |
| Migrations didn't apply but prod just got restarted | Check `MIGRATE_ON_STARTUP=false` — that's the gate doing its job. Run §1 |
| Backups stopped | `sudo tail /var/log/qalam-mssql-backup.log` — usually disk full or SA password rotated without updating `/root/.mssql-sa` |
| Certbot renewal failing | DNS record gone, or port 80 not reachable. `sudo certbot renew --dry-run -v` for the full trace |

---

## 11. Useful one-shots

```bash
# Disk usage by Docker
docker system df

# Reclaim space (safe — won't touch named volumes)
docker system prune -f

# How big are the backups
du -sh /var/opt/mssql/backups

# List active connections per database
sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C -Q \
  "SELECT DB_NAME(dbid) AS db, COUNT(*) AS sessions FROM sys.sysprocesses WHERE dbid > 0 GROUP BY dbid;"

# Show pending migrations for a DB (run on the VPS host, requires dotnet ef tools)
# Easiest path: connect with sqlcmd and query __EFMigrationsHistory directly.
sqlcmd -S 127.0.0.1 -U qalam_prod_user -P '...' -d qalam_prod -C -Q \
  "SELECT TOP 5 MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId DESC;"
```
