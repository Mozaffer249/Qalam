# After `sql-bootstrap-qalam.sql` — step-by-step

> **Deployment · Step 2 of 5** · [Index](./README.md) · Prev: [01-sql-server-install.md](./01-sql-server-install.md) · Next: [03-staging-setup.md](./03-staging-setup.md)

Run this on the **VPS** after you have finished [`01-sql-server-install.md`](./01-sql-server-install.md) sections 1–5 and executed:

```sh
sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C -i docs/deployment/sql-bootstrap-qalam.sql
```

SQL logins use password **`Qalam@2026`** (`qalam_staging_user` / `qalam_prod_user`).

---

## Step 1 — Confirm bootstrap worked

From the shell (not inside interactive `sqlcmd`):

```sh
sqlcmd -S 127.0.0.1 -U SA -P "$(sudo cat /root/.mssql-sa)" -C -Q "
SELECT name FROM sys.server_principals WHERE name IN ('qalam_staging_user','qalam_prod_user');
SELECT name FROM sys.databases WHERE name IN ('qalam_staging','qalam_messaging_staging','qalam_prod','qalam_messaging_prod');
"
```

Expect **2** login rows and **4** database rows.

---

## Step 2 — Smoke-test SQL logins

```sh
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P 'Qalam@2026' -d qalam_staging -C -Q "SELECT DB_NAME();"
sqlcmd -S 127.0.0.1 -U qalam_prod_user    -P 'Qalam@2026'    -d qalam_prod    -C -Q "SELECT DB_NAME();"
```

Each should print one row: `qalam_staging` and `qalam_prod`.

Staging must **not** access prod (error 916 is OK):

```sh
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P 'Qalam@2026' -d qalam_prod -C -Q "SELECT 1"
```

---

## Step 3 — (Optional) Nightly SQL backups

If you have not done [`01-sql-server-install.md`](./01-sql-server-install.md) §8 yet, run the backup script + cron there once. Skip if already configured.

---

## Step 4 — DNS for staging API

At your DNS host, add:

| Host | Type | Value |
|------|------|-------|
| `api-staging` | A | `8.213.80.90` |

(Full name: `api-staging.qalam.net.sa`.)

From your laptop:

```sh
dig +short api-staging.qalam.net.sa
# Expect: 8.213.80.90
```

Wait until DNS resolves before Certbot (Step 9).

---

## Step 5 — Use your existing repo (no second clone)

Staging and production share **one** git clone on the VPS. If production is already at `/opt/qalam-backend/Qalam`, use that path (adjust if yours differs).

```sh
cd /opt/qalam-backend/Qalam    # your existing prod clone
git pull                       # get latest docs/deployment/* and compose files
```

You will run **two** Compose projects from this directory:

| Stack | Compose file | Project name | Env file | Host port |
|-------|----------------|--------------|----------|-----------|
| Production | `docker-compose.prod.yml` | `qalam-prod` (or your existing name) | `.env` or `.env.prod` | `8080` |
| Staging | `docker-compose.staging.yml` | `qalam-staging` | `.env.staging` | `8081` |

They do not conflict — separate containers, networks, and env files in the same folder.

---

## Step 6 — Create `.env.staging`

In the **same** repo directory as production:

```sh
cd /opt/qalam-backend/Qalam
cp .env.staging.example .env.staging
chmod 600 .env.staging
nano .env.staging
```

Set at minimum:

| Variable | What to put |
|----------|-------------|
| `DB_CONNECTION_STRING` | Keep template; set `Password=Qalam@2026` (replace both `REPLACE_ME`) |
| `MESSAGING_DB_CONNECTION_STRING` | Same: `Password=Qalam@2026` |
| `ENCRYPTION_KEY` | `openssl rand -hex 16` |
| `JWT_SECRET` | `openssl rand -base64 48` |
| `RABBITMQ_PASS` | `openssl rand -base64 24` |
| `DEFAULT_ADMIN_PASSWORD` | Strong password for first admin login |
| `EMAIL_FROM_EMAIL`, `EMAIL_USERNAME`, `EMAIL_PASSWORD` | SMTP (Mailtrap OK for staging) |

Example connection strings (copy-paste if easier):

```text
DB_CONNECTION_STRING=Server=host.docker.internal,1433;Database=qalam_staging;User Id=qalam_staging_user;Password=Qalam@2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
MESSAGING_DB_CONNECTION_STRING=Server=host.docker.internal,1433;Database=qalam_messaging_staging;User Id=qalam_staging_user;Password=Qalam@2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

Verify permissions:

```sh
ls -la .env.staging
# expect: -rw------- ... .env.staging
```

---

## Step 7 — Start staging Docker stack

```sh
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build
```

Production can keep running (`127.0.0.1:8080`); staging binds `127.0.0.1:8081` only.

Watch API logs until migrations + seed finish:

```sh
docker compose -f docker-compose.staging.yml -p qalam-staging logs -f qalam-api
# Look for: Database seeding completed successfully!
```

Check containers:

```sh
docker compose -p qalam-staging ps
# Expect 3 services: rabbitmq, messaging-api, backend-api — all running
```

Local health check on the VPS:

```sh
curl -sS http://127.0.0.1:8081/health
# Expect: HTTP 200
```

If the API exits immediately, check logs for `ConnectionStrings` or SQL login errors — password must be exactly `Qalam@2026` in `.env.staging`.

---

## Step 8 — Nginx reverse proxy (HTTP first)

```sh
sudo apt install -y nginx certbot python3-certbot-nginx

sudo tee /etc/nginx/sites-available/api-staging.qalam.net.sa > /dev/null <<'EOF'
server {
    listen 80;
    server_name api-staging.qalam.net.sa;

    client_max_body_size 25M;

    location / {
        proxy_pass http://127.0.0.1:8081;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 90s;
    }
}
EOF

sudo ln -sf /etc/nginx/sites-available/api-staging.qalam.net.sa /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

---

## Step 9 — HTTPS (Certbot)

```sh
sudo certbot --nginx -d api-staging.qalam.net.sa --redirect --agree-tos -m you@example.com
sudo certbot renew --dry-run
```

---

## Step 10 — Smoke tests (laptop or VPS)

```sh
curl -sS https://api-staging.qalam.net.sa/health

curl -sS -X POST https://api-staging.qalam.net.sa/Api/V1/Authentication/Login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@staging.qalam.net.sa","password":"YOUR_DEFAULT_ADMIN_PASSWORD"}'
```

Swagger (browser): `https://api-staging.qalam.net.sa/swagger`

Confirm EF created tables (on VPS):

```sh
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P 'Qalam@2026' -d qalam_staging -C -Q \
  "SELECT COUNT(*) AS table_count FROM sys.tables;"
```

---

## Step 11 — Staging checklist

- [ ] Step 1: 2 logins + 4 databases
- [ ] Step 2: staging/prod logins connect; cross-env denied
- [ ] Step 7: `docker compose -p qalam-staging ps` → 3 running
- [ ] Step 7: `curl http://127.0.0.1:8081/health` → 200
- [ ] Step 10: `https://api-staging.qalam.net.sa/health` → 200
- [ ] Admin login works with `DEFAULT_ADMIN_PASSWORD` from `.env.staging`
- [ ] `.env.staging` is `chmod 600`

Then rotate the demo admin password ([`03-staging-setup.md`](./03-staging-setup.md) §7) and set `SEED_DEFAULT_ADMIN=false` when done testing.

---

## Step 12 — Production (after staging is green)

Prod uses the **same SQL password** in this bootstrap (`Qalam@2026`) but login **`qalam_prod_user`** and DBs `qalam_prod` / `qalam_messaging_prod`.

Follow [`04-production-setup.md`](./04-production-setup.md):

1. Use the **same** repo as staging (`/opt/qalam-backend/Qalam`); add or update `.env.prod` (or your existing `.env`).
2. Set `Password=Qalam@2026` on prod connection strings (`qalam_prod_user` / `qalam_prod` DBs).
3. **Different** `ENCRYPTION_KEY`, `JWT_SECRET`, `RABBITMQ_PASS` from staging (never reuse).
4. Manual migrations on first deploy (`MIGRATE_ON_STARTUP=false`).
5. Nginx + Certbot for `api.qalam.net.sa` → `127.0.0.1:8080`.

Full Nginx/subdomain map: [`05-nginx-subdomains.md`](./05-nginx-subdomains.md).

---

## Quick reference

| Item | Staging | Production |
|------|---------|------------|
| SQL user | `qalam_staging_user` | `qalam_prod_user` |
| SQL password | `Qalam@2026` | `Qalam@2026` |
| Main DB | `qalam_staging` | `qalam_prod` |
| Messaging DB | `qalam_messaging_staging` | `qalam_messaging_prod` |
| API local port | `8081` | `8080` |
| Public URL | `https://api-staging.qalam.net.sa` | `https://api.qalam.net.sa` |
| Compose | `docker-compose.staging.yml` | `docker-compose.prod.yml` |
| Project name | `qalam-staging` | `qalam-prod` |
