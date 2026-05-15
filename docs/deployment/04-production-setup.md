# Production Setup Guide

> **Deployment · Step 4 of 5** · [Index](./README.md) · Prev: [03-staging-setup.md](./03-staging-setup.md) · Next: [05-nginx-subdomains.md](./05-nginx-subdomains.md)

Brings up the **production** docker-compose stack at `https://api.qalam.net.sa`. Same VPS, same SQL Server, but isolated from staging via separate databases, Compose project, network, and Nginx vhost.

Prerequisites:
- [`01-sql-server-install.md`](./01-sql-server-install.md) is complete (the `qalam_prod` / `qalam_messaging_prod` DBs + `qalam_prod_user` login exist).
- [`03-staging-setup.md`](./03-staging-setup.md) is green — you've validated the build on staging first.

Index: [`README.md`](./README.md).

The key differences vs staging: **manual migrations**, **default-admin seeding is gated off after first boot**, and **stricter CORS**.

---

## 1. DNS check

`api.qalam.net.sa` should already resolve to `8.213.80.90` (you confirmed this when you provided the existing DNS records). Verify:

```bash
dig +short api.qalam.net.sa
# Expect: 8.213.80.90
```

---

## 2. Use your existing repo

Production should already be cloned (e.g. `/opt/qalam-backend/Qalam`). Staging uses the **same directory** with `.env.staging` and `docker-compose.staging.yml`; prod uses `.env` / `.env.prod` and `docker-compose.prod.yml`.

```bash
cd /opt/qalam-backend/Qalam    # adjust to your path
git pull

# Pin prod to the tag you validated on staging when you are ready to release.
# git checkout v0.1.0
```

Skip a second clone under `/opt/qalam-prod` unless you deliberately want isolated directories.

---

## 3. Configure `.env.prod`

```bash
cp .env.prod.example .env.prod
chmod 600 .env.prod
nano .env.prod
```

Required fills:

| Variable | Value |
|----------|-------|
| `DB_CONNECTION_STRING` | Set `Password=Qalam@2026` (user `qalam_prod_user`, DB `qalam_prod`) |
| `MESSAGING_DB_CONNECTION_STRING` | Same password; DB `qalam_messaging_prod` |
| `ENCRYPTION_KEY` | `openssl rand -hex 16` — **fresh, do not reuse staging** |
| `JWT_SECRET` | `openssl rand -base64 48` — **fresh** |
| `RABBITMQ_PASS` | `openssl rand -base64 24` |
| `MIGRATE_ON_STARTUP` | `false` (default) — keep it off |
| `SEED_DEFAULT_ADMIN` | `true` **only for the very first boot**; flip to `false` immediately after |
| `DEFAULT_ADMIN_PASSWORD` | One-time strong password; you'll rotate via API and unset this within an hour |
| `EMAIL_*` | Real SMTP — bounces/OTP must be reliable |
| `CORS_ALLOWED_ORIGINS` | Locked to your production frontends (already pre-filled with `qalam.net.sa`, `teacher.qalam.net.sa`, `admin.qalam.net.sa`) |

---

## 4. Apply migrations manually (first deploy)

`MIGRATE_ON_STARTUP=false` means the API will **not** create the schema on its own. Run migrations as a one-off, then start the long-lived stack.

```bash
cd /opt/qalam-backend/Qalam

# Bring up only the API container with MIGRATE_ON_STARTUP=true temporarily,
# wait for it to apply migrations, then take it down.
docker compose -f docker-compose.prod.yml -p qalam-prod-migrate --env-file .env.prod \
  run --rm \
  -e MIGRATE_ON_STARTUP=true \
  -e SEED_DEFAULT_ADMIN=false \
  qalam-api dotnet Qalam.Api.dll --apply-migrations-only || true
```

> The container's normal entrypoint runs the API, which on success will hang. The simpler reliable path is to start the stack with `MIGRATE_ON_STARTUP=true` for the very first boot, then immediately update `.env.prod` to `false` and recreate:

```bash
# Option A: one-shot first boot with auto-migrate
sed -i 's/^MIGRATE_ON_STARTUP=false/MIGRATE_ON_STARTUP=true/' .env.prod
sed -i 's/^SEED_DEFAULT_ADMIN=false/SEED_DEFAULT_ADMIN=true/' .env.prod

docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --build

# Wait for "Database seeding completed successfully" in the logs.
docker compose -p qalam-prod logs -f qalam-api

# Then immediately revert the gates.
sed -i 's/^MIGRATE_ON_STARTUP=true/MIGRATE_ON_STARTUP=false/' .env.prod
sed -i 's/^SEED_DEFAULT_ADMIN=true/SEED_DEFAULT_ADMIN=false/' .env.prod

# Clear the seeded password from disk.
sed -i 's/^DEFAULT_ADMIN_PASSWORD=.*/DEFAULT_ADMIN_PASSWORD=/' .env.prod

# Recreate the API container so the new (locked-down) env takes effect.
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --force-recreate qalam-api
```

From here on, every subsequent migration is applied via the [Operations Runbook](../OPERATIONS_RUNBOOK.md) procedure, **not** by restarting the API.

---

## 5. Nginx vhost + TLS

```bash
sudo tee /etc/nginx/sites-available/api.qalam.net.sa > /dev/null <<'EOF'
server {
    listen 80;
    server_name api.qalam.net.sa;
    client_max_body_size 25M;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 90s;
    }
}
EOF

sudo ln -sf /etc/nginx/sites-available/api.qalam.net.sa /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

sudo certbot --nginx -d api.qalam.net.sa --redirect --agree-tos -m you@example.com
sudo certbot renew --dry-run
```

---

## 6. Smoke tests (from your laptop)

```bash
# Health
curl https://api.qalam.net.sa/health
# Expect: HTTP/2 200

# Login with the seeded admin (only valid once — you'll rotate it now)
curl -sS -X POST https://api.qalam.net.sa/Api/V1/Authentication/Login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@qalam.net.sa","password":"YOUR_ONE_TIME_PASSWORD"}' \
  | jq .
```

Use the returned token to call the password-change endpoint and set the real admin password.

---

## 7. Side-by-side check

Both stacks must run simultaneously without colliding:

```bash
docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
# Expect 6 containers:
#   qalam-staging-rabbitmq          Up ...     (no host ports)
#   qalam-staging-messaging-api     Up ...     (no host ports)
#   qalam-staging-backend-api       Up ...     127.0.0.1:8081->80/tcp
#   qalam-rabbitmq                  Up ...     (no host ports)
#   qalam-messaging-api             Up ...     (no host ports)
#   qalam-backend-api               Up ...     127.0.0.1:8080->80/tcp
```

---

## 8. Verification checklist

- [ ] `curl https://api.qalam.net.sa/health` → 200.
- [ ] `docker compose -p qalam-prod logs qalam-api | grep "Skipping auto-migration"` is present **after** the first boot — proves the gate works.
- [ ] `.env.prod` no longer contains `DEFAULT_ADMIN_PASSWORD=…` after the rotation.
- [ ] `MIGRATE_ON_STARTUP=false` and `SEED_DEFAULT_ADMIN=false` in `.env.prod`.
- [ ] Six containers visible in `docker ps`; only 8080 + 8081 bind to `127.0.0.1`, nothing on `0.0.0.0`.
- [ ] `sudo certbot certificates` lists both `api.qalam.net.sa` and `api-staging.qalam.net.sa`.
- [ ] Admin password has been rotated via the API.

After this, day-2 ops live in [`OPERATIONS_RUNBOOK.md`](../OPERATIONS_RUNBOOK.md).
