# SQL Server Install Guide — Ubuntu 22.04

> **Deployment · Step 1 of 5** · [Index](./README.md) · Next: run [`sql-bootstrap-qalam.sql`](./sql-bootstrap-qalam.sql), then [02-after-sql-bootstrap.md](./02-after-sql-bootstrap.md)

Installs SQL Server 2022 natively on the VPS, listening on **127.0.0.1:1433 only**, and creates the four databases + two SQL logins used by the staging and production stacks.

The database engine is **not** exposed to the internet. Containers reach it via `host.docker.internal`, which the compose files map back to the host loopback.

Run all commands as a user with `sudo` (don't run the whole script as root unless noted).

---

## 1. Prerequisites

Confirm the OS and that the VPS has at least **2 GB RAM** (SQL Server's hard minimum is 2 GB; 4 GB+ recommended once both stacks run).

```sh
lsb_release -a               # Ubuntu 22.04 LTS expected
free -h                      # 2 GB RAM minimum, 4 GB+ recommended
df -h /var                   # need ~6 GB free for the install + system DBs
sudo apt update && sudo apt upgrade -y
```

If you don't already have `curl`, `gnupg`, `apt-transport-https`:

```sh
sudo apt install -y curl gnupg apt-transport-https ca-certificates
```

---

## 2. Add Microsoft's apt repository

```sh
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
  | sudo gpg --dearmor -o /etc/apt/trusted.gpg.d/microsoft.gpg

curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list \
  | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list

curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/prod.list \
  | sudo tee /etc/apt/sources.list.d/msprod.list

sudo apt update
```

---

## 3. Install the server

```sh
sudo apt install -y mssql-server
```

Then run the interactive setup. Choose **Developer edition** if this VPS hosts only staging-data; choose **Express** for tiny production loads (10 GB per-DB cap); license **Standard** for real production usage. Set a strong SA password — Microsoft requires 8+ chars with mixed case, digit, and symbol.

```sh
sudo /opt/mssql/bin/mssql-conf setup
```

Save the SA password somewhere safe on the host only:

```sh
sudo touch /root/.mssql-sa && sudo chmod 600 /root/.mssql-sa
echo 'YOUR_SA_PASSWORD' | sudo tee /root/.mssql-sa > /dev/null
```

Verify the service is running:

```sh
systemctl status mssql-server   # expect "active (running)"
```

---

## 4. Install the client tools (sqlcmd / bcp)

```sh
sudo apt install -y mssql-tools18 unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
source ~/.bashrc
sqlcmd -?              # confirm sqlcmd is on PATH
```

---

## 5. Bind the listener to 127.0.0.1 only

By default SQL Server listens on `0.0.0.0:1433`. We want it loopback-only so nothing outside the VPS can ever reach it.

```sh
sudo /opt/mssql/bin/mssql-conf set network.ipaddress 127.0.0.1
sudo /opt/mssql/bin/mssql-conf set network.tcpport 1433
sudo systemctl restart mssql-server
```

Verify:

```sh
ss -tlnp | grep 1433
# Expected:  LISTEN 0  ... 127.0.0.1:1433  ...  users:(("sqlservr",...))
# WRONG (don't leave it like this): 0.0.0.0:1433 or :::1433
```

Confirm from another machine that 1433 is not reachable:

```sh
# Run this from your laptop, NOT the VPS:
nc -zv 8.213.80.90 1433
# Expected: "Connection refused" or timeout. Anything that succeeds is a misconfiguration.
```

If your VPS provider has its own firewall (e.g., Hostinger panel firewall), make sure 1433 is NOT in the allow list. Local UFW should also leave it closed:

```sh
sudo ufw status verbose | grep 1433   # expect no rule
```

---

## 6. Create the four databases and two SQL logins

This deployment uses SQL login password **`Qalam@2026`** for both `qalam_staging_user` and `qalam_prod_user` (same password, separate logins and databases). Put the same value in:

- Same repo, e.g. `/opt/qalam-backend/Qalam/.env.staging` → `Password=Qalam@2026` (staging DBs)
- Same repo, `.env` or `.env.prod` → `Password=Qalam@2026` (prod DBs)

> **Security:** `Qalam@2026` is documented here for your current VPS setup. Rotate to unique strong passwords per environment before real production traffic.

### Option A — Run the bootstrap script (recommended)

From your existing repo root on the VPS (e.g. `/opt/qalam-backend/Qalam`):

```sh
cd /opt/qalam-backend/Qalam   # adjust path to your clone

sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C -i docs/deployment/sql-bootstrap-qalam.sql
```

**Next:** [`02-after-sql-bootstrap.md`](./02-after-sql-bootstrap.md) — verify logins, configure `.env.staging`, start Docker, Nginx, and HTTPS.

The script is idempotent: safe to re-run if you only created `qalam_staging` earlier or need to reset passwords.

### Option B — Interactive sqlcmd (paste SQL only)

```sh
sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C
```

At the `1>` prompt, paste **only SQL** (not shell commands). End with `EXIT`.

```sql
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qalam_staging')
    CREATE DATABASE qalam_staging;
GO
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qalam_messaging_staging')
    CREATE DATABASE qalam_messaging_staging;
GO
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qalam_prod')
    CREATE DATABASE qalam_prod;
GO
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'qalam_messaging_prod')
    CREATE DATABASE qalam_messaging_prod;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'qalam_staging_user')
    CREATE LOGIN qalam_staging_user WITH PASSWORD = 'Qalam@2026';
ELSE
    ALTER LOGIN qalam_staging_user WITH PASSWORD = 'Qalam@2026';
GO

USE qalam_staging;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'qalam_staging_user')
BEGIN
    CREATE USER qalam_staging_user FOR LOGIN qalam_staging_user;
    ALTER ROLE db_owner ADD MEMBER qalam_staging_user;
END
GO

USE qalam_messaging_staging;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'qalam_staging_user')
BEGIN
    CREATE USER qalam_staging_user FOR LOGIN qalam_staging_user;
    ALTER ROLE db_owner ADD MEMBER qalam_staging_user;
END
GO

USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'qalam_prod_user')
    CREATE LOGIN qalam_prod_user WITH PASSWORD = 'Qalam@2026';
ELSE
    ALTER LOGIN qalam_prod_user WITH PASSWORD = 'Qalam@2026';
GO

USE qalam_prod;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'qalam_prod_user')
BEGIN
    CREATE USER qalam_prod_user FOR LOGIN qalam_prod_user;
    ALTER ROLE db_owner ADD MEMBER qalam_prod_user;
END
GO

USE qalam_messaging_prod;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'qalam_prod_user')
BEGIN
    CREATE USER qalam_prod_user FOR LOGIN qalam_prod_user;
    ALTER ROLE db_owner ADD MEMBER qalam_prod_user;
END
GO

EXIT
```

### Verify objects exist (from bash, not inside `1>`)

```sh
sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C -Q "
SELECT name FROM sys.server_principals WHERE name IN ('qalam_staging_user','qalam_prod_user');
SELECT name FROM sys.databases WHERE name IN ('qalam_staging','qalam_messaging_staging','qalam_prod','qalam_messaging_prod');
"
```

Expect **2** login rows and **4** database rows.

Each login is `db_owner` **only on its own two databases** — cross-environment access is blocked at the SQL layer.

---

## 7. Smoke test the new logins

Run these from the **shell** (`root@...:~#`), **not** inside interactive `sqlcmd` (`1>`).

```sh
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P 'Qalam@2026' -d qalam_staging -C -Q "SELECT @@VERSION; SELECT DB_NAME();"
sqlcmd -S 127.0.0.1 -U qalam_prod_user    -P 'Qalam@2026'    -d qalam_prod    -C -Q "SELECT @@VERSION; SELECT DB_NAME();"
```

Both should print the SQL Server 2022 banner and `qalam_staging` / `qalam_prod`.

Confirm the staging user **cannot** see the prod DB:

```sh
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P 'Qalam@2026' -d qalam_prod -C -Q "SELECT 1"
# Expected: error 916 — "The server principal ... is not able to access the database 'qalam_prod' under the current security context."
```

### Connection string examples (for `.env` files)

Staging (`Password=Qalam@2026`):

```text
Server=host.docker.internal,1433;Database=qalam_staging;User Id=qalam_staging_user;Password=Qalam@2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
Server=host.docker.internal,1433;Database=qalam_messaging_staging;User Id=qalam_staging_user;Password=Qalam@2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

Production (`Password=Qalam@2026`, user `qalam_prod_user`, databases `qalam_prod` / `qalam_messaging_prod`):

```text
Server=host.docker.internal,1433;Database=qalam_prod;User Id=qalam_prod_user;Password=Qalam@2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
Server=host.docker.internal,1433;Database=qalam_messaging_prod;User Id=qalam_prod_user;Password=Qalam@2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

---

## 8. Backup directory + nightly cron

```sh
sudo mkdir -p /var/opt/mssql/backups
sudo chown mssql:mssql /var/opt/mssql/backups
sudo chmod 750 /var/opt/mssql/backups
```

Install the nightly backup script (run as root):

```sh
sudo tee /usr/local/bin/qalam-mssql-backup.sh > /dev/null <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
SA_PWD="$(cat /root/.mssql-sa)"
TS="$(date +%Y%m%d_%H%M%S)"
BACKUP_DIR=/var/opt/mssql/backups

for DB in qalam_staging qalam_messaging_staging qalam_prod qalam_messaging_prod; do
  OUT="${BACKUP_DIR}/${DB}_${TS}.bak"
  # NOTE: no WITH COMPRESSION — not supported on Express Edition. gzip below handles compression instead.
  /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U SA -P "$SA_PWD" -C \
    -Q "BACKUP DATABASE [${DB}] TO DISK = N'${OUT}' WITH NOFORMAT, NOINIT, NAME = '${DB}-${TS}', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
  gzip -f "${OUT}"
done

# Retain 14 days locally; offsite copy is a follow-up (rclone to S3/Wasabi).
find "${BACKUP_DIR}" -name '*.bak.gz' -mtime +14 -delete
EOF

sudo chmod 700 /usr/local/bin/qalam-mssql-backup.sh
```

Schedule it nightly at 02:00:

```sh
echo '0 2 * * * root /usr/local/bin/qalam-mssql-backup.sh >> /var/log/qalam-mssql-backup.log 2>&1' \
  | sudo tee /etc/cron.d/qalam-mssql-backup
sudo chmod 644 /etc/cron.d/qalam-mssql-backup
```

Run it once manually to confirm:

```sh
sudo /usr/local/bin/qalam-mssql-backup.sh
ls -lh /var/opt/mssql/backups/
# Expect 4 .bak.gz files owned by mssql:mssql.
```

---

## 9. Verification checklist

- [ ] `systemctl status mssql-server` → active.
- [ ] `ss -tlnp | grep 1433` → bound to `127.0.0.1`, not `0.0.0.0`.
- [ ] From a remote host: `nc -zv <vps-ip> 1433` → connection refused.
- [ ] `sqlcmd -S 127.0.0.1 -U qalam_staging_user -P ... -d qalam_staging -C -Q "SELECT 1"` returns `1`.
- [ ] Same with `qalam_prod_user` / `qalam_prod`.
- [ ] Cross-env access denied (error 916).
- [ ] `/var/opt/mssql/backups/` contains four `.bak.gz` files after running the backup script.
- [ ] `/etc/cron.d/qalam-mssql-backup` exists with mode 644.

When all boxes are checked, follow **[`02-after-sql-bootstrap.md`](./02-after-sql-bootstrap.md)** for the numbered steps (verify SQL → staging Docker → Nginx → HTTPS). Long-form detail: [`03-staging-setup.md`](./03-staging-setup.md).

Index: [`README.md`](./README.md)