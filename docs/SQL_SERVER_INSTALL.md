# SQL Server Install Guide — Ubuntu 22.04

Installs SQL Server 2022 natively on the VPS, listening on **127.0.0.1:1433 only**, and creates the four databases + two SQL logins used by the staging and production stacks.

The database engine is **not** exposed to the internet. Containers reach it via `host.docker.internal`, which the compose files map back to the host loopback.

Run all commands as a user with `sudo` (don't run the whole script as root unless noted).

---

## 1. Prerequisites

Confirm the OS and that the VPS has at least **2 GB RAM** (SQL Server's hard minimum is 2 GB; 4 GB+ recommended once both stacks run).

```bash
lsb_release -a               # Ubuntu 22.04 LTS expected
free -h                      # 2 GB RAM minimum, 4 GB+ recommended
df -h /var                   # need ~6 GB free for the install + system DBs
sudo apt update && sudo apt upgrade -y
```

If you don't already have `curl`, `gnupg`, `apt-transport-https`:

```bash
sudo apt install -y curl gnupg apt-transport-https ca-certificates
```

---

## 2. Add Microsoft's apt repository

```bash
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

```bash
sudo apt install -y mssql-server
```

Then run the interactive setup. Choose **Developer edition** if this VPS hosts only staging-data; choose **Express** for tiny production loads (10 GB per-DB cap); license **Standard** for real production usage. Set a strong SA password — Microsoft requires 8+ chars with mixed case, digit, and symbol.

```bash
sudo /opt/mssql/bin/mssql-conf setup
```

Save the SA password somewhere safe on the host only:

```bash
sudo touch /root/.mssql-sa && sudo chmod 600 /root/.mssql-sa
echo 'YOUR_SA_PASSWORD' | sudo tee /root/.mssql-sa > /dev/null
```

Verify the service is running:

```bash
systemctl status mssql-server   # expect "active (running)"
```

---

## 4. Install the client tools (sqlcmd / bcp)

```bash
sudo apt install -y mssql-tools18 unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
source ~/.bashrc
sqlcmd -?              # confirm sqlcmd is on PATH
```

---

## 5. Bind the listener to 127.0.0.1 only

By default SQL Server listens on `0.0.0.0:1433`. We want it loopback-only so nothing outside the VPS can ever reach it.

```bash
sudo /opt/mssql/bin/mssql-conf set network.ipaddress 127.0.0.1
sudo /opt/mssql/bin/mssql-conf set network.tcpport 1433
sudo systemctl restart mssql-server
```

Verify:

```bash
ss -tlnp | grep 1433
# Expected:  LISTEN 0  ... 127.0.0.1:1433  ...  users:(("sqlservr",...))
# WRONG (don't leave it like this): 0.0.0.0:1433 or :::1433
```

Confirm from another machine that 1433 is not reachable:

```bash
# Run this from your laptop, NOT the VPS:
nc -zv 8.213.80.90 1433
# Expected: "Connection refused" or timeout. Anything that succeeds is a misconfiguration.
```

If your VPS provider has its own firewall (e.g., Hostinger panel firewall), make sure 1433 is NOT in the allow list. Local UFW should also leave it closed:

```bash
sudo ufw status verbose | grep 1433   # expect no rule
```

---

## 6. Create the four databases and two SQL logins

Generate two strong passwords now — one per environment — and keep them only in `/opt/qalam-staging/.env.staging` and `/opt/qalam-prod/.env.prod` (which you create later).

```bash
openssl rand -base64 24   # staging user password
openssl rand -base64 24   # prod user password
```

Connect as SA and run the bootstrap SQL:

```bash
sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C
```

Then paste the following inside the `1>` prompt. Replace the two `REPLACE_…` passwords with the ones you just generated. The `GO` lines are required separators in sqlcmd.

```sql
-- Staging
CREATE DATABASE qalam_staging;
GO
CREATE DATABASE qalam_messaging_staging;
GO
CREATE LOGIN qalam_staging_user WITH PASSWORD = 'REPLACE_STAGING_PASSWORD';
GO

USE qalam_staging;
GO
CREATE USER qalam_staging_user FOR LOGIN qalam_staging_user;
ALTER ROLE db_owner ADD MEMBER qalam_staging_user;
GO

USE qalam_messaging_staging;
GO
CREATE USER qalam_staging_user FOR LOGIN qalam_staging_user;
ALTER ROLE db_owner ADD MEMBER qalam_staging_user;
GO

-- Production
USE master;
GO
CREATE DATABASE qalam_prod;
GO
CREATE DATABASE qalam_messaging_prod;
GO
CREATE LOGIN qalam_prod_user WITH PASSWORD = 'REPLACE_PROD_PASSWORD';
GO

USE qalam_prod;
GO
CREATE USER qalam_prod_user FOR LOGIN qalam_prod_user;
ALTER ROLE db_owner ADD MEMBER qalam_prod_user;
GO

USE qalam_messaging_prod;
GO
CREATE USER qalam_prod_user FOR LOGIN qalam_prod_user;
ALTER ROLE db_owner ADD MEMBER qalam_prod_user;
GO

EXIT
```

Each login is `db_owner` **only on its own two databases** — cross-environment access is blocked at the SQL layer.

---

## 7. Smoke test the new logins

```bash
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P 'REPLACE_STAGING_PASSWORD' -d qalam_staging -C -Q "SELECT @@VERSION; SELECT DB_NAME();"
sqlcmd -S 127.0.0.1 -U qalam_prod_user    -P 'REPLACE_PROD_PASSWORD'    -d qalam_prod    -C -Q "SELECT @@VERSION; SELECT DB_NAME();"
```

Both should print the SQL Server 2022 banner and the correct DB name.

Confirm the staging user **cannot** see the prod DB:

```bash
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P 'REPLACE_STAGING_PASSWORD' -d qalam_prod -C -Q "SELECT 1"
# Expected: error 916 — "The server principal ... is not able to access the database 'qalam_prod' under the current security context."
```

---

## 8. Backup directory + nightly cron

```bash
sudo mkdir -p /var/opt/mssql/backups
sudo chown mssql:mssql /var/opt/mssql/backups
sudo chmod 750 /var/opt/mssql/backups
```

Install the nightly backup script (run as root):

```bash
sudo tee /usr/local/bin/qalam-mssql-backup.sh > /dev/null <<'EOF'
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

# Retain 14 days locally; offsite copy is a follow-up (rclone to S3/Wasabi).
find "${BACKUP_DIR}" -name '*.bak.gz' -mtime +14 -delete
EOF

sudo chmod 700 /usr/local/bin/qalam-mssql-backup.sh
```

Schedule it nightly at 02:00:

```bash
echo '0 2 * * * root /usr/local/bin/qalam-mssql-backup.sh >> /var/log/qalam-mssql-backup.log 2>&1' \
  | sudo tee /etc/cron.d/qalam-mssql-backup
sudo chmod 644 /etc/cron.d/qalam-mssql-backup
```

Run it once manually to confirm:

```bash
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

When all boxes are checked, move on to [`STAGING_SETUP.md`](./STAGING_SETUP.md).
