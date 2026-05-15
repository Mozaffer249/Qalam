# Staging Setup Guide

> **Deployment · Step 3 of 5** · [Index](./README.md) · Prev: [02-after-sql-bootstrap.md](./02-after-sql-bootstrap.md) · Next: [04-production-setup.md](./04-production-setup.md)

Brings up the **staging** docker-compose stack at `https://api-staging.qalam.net.sa`. Runs side-by-side with production on the same VPS and **the same git clone** (e.g. `/opt/qalam-backend/Qalam`), with separate `.env.staging`, Compose project `qalam-staging`, databases, RabbitMQ, and Nginx vhost.

Prerequisites: [`01-sql-server-install.md`](./01-sql-server-install.md) is complete (run `docs/deployment/sql-bootstrap-qalam.sql`). Quick path: [`02-after-sql-bootstrap.md`](./02-after-sql-bootstrap.md). Index: [`README.md`](./README.md).

---

## 1. DNS record

Add **one** A record at your DNS host:

```
api-staging.qalam.net.sa.   A   8.213.80.90   300
```

Confirm propagation from your laptop:

```sh
dig +short api-staging.qalam.net.sa
# Expect: 8.213.80.90
```

Wait for TTL before continuing — Certbot's HTTP-01 challenge needs DNS resolution.

---

## 2. Use your existing repo (one clone for prod + staging)

You do **not** need a second clone under `/opt/qalam-staging`. Use the directory where production already lives:

```sh
cd /opt/qalam-backend/Qalam    # adjust to your path
git pull
```

Optional: run Docker as a `deploy` user instead of root — only if you are not already set up:

```sh
sudo usermod -aG docker $USER   # then log out and back in
```

Staging and prod both run from this folder with different env files and Compose project names (`-p qalam-staging` vs your prod project).

---

## 3. Configure `.env.staging`

```sh
cp .env.staging.example .env.staging
chmod 600 .env.staging
nano .env.staging
```

Required fills (everything else in the template has sensible defaults):

| Variable | Value |
|----------|-------|
| `DB_CONNECTION_STRING` | Set `Password=Qalam@2026` (user `qalam_staging_user`, DB `qalam_staging`) — see [`02-after-sql-bootstrap.md`](./02-after-sql-bootstrap.md) Step 6 |
| `MESSAGING_DB_CONNECTION_STRING` | Same password; DB `qalam_messaging_staging` |
| `ENCRYPTION_KEY` | `openssl rand -hex 16` |
| `JWT_SECRET` | `openssl rand -base64 48` |
| `RABBITMQ_PASS` | `openssl rand -base64 24` |
| `DEFAULT_ADMIN_PASSWORD` | Strong password (you'll log in with this once, then rotate via the API) |
| `EMAIL_FROM_EMAIL`, `EMAIL_USERNAME`, `EMAIL_PASSWORD` | SMTP creds (Mailtrap sandbox is fine for staging) |

Verify the file is locked down:

```sh
ls -la .env.staging
# expect: -rw------- 1 deploy deploy ... .env.staging
```

---

## 4. Bring up the stack

```sh
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build
```

First boot does three things, in this order:

1. RabbitMQ container starts.
2. MessagingApi connects to RabbitMQ + `qalam_messaging_staging`, runs migrations on that DB.
3. Qalam API connects to `qalam_staging`, runs migrations (`MIGRATE_ON_STARTUP=true` for staging), seeds reference data + roles + the demo admin (`SEED_DEFAULT_ADMIN=true`).

Watch logs until you see `Database seeding completed successfully!`:

```sh
docker compose -f docker-compose.staging.yml -p qalam-staging logs -f qalam-api
```

Confirm three containers are up:

```sh
docker compose -p qalam-staging ps
# Expect: qalam-staging-rabbitmq, qalam-staging-messaging-api, qalam-staging-backend-api — all "running".
```

Quick local check (still on the VPS):

```sh
curl http://127.0.0.1:8081/health
# Expect: 200 OK with health payload
```

---

## 5. Nginx vhost + TLS cert

Install Nginx and Certbot if not already on the box:

```sh
sudo apt install -y nginx certbot python3-certbot-nginx
```

Create the staging vhost:

```sh
sudo tee /etc/nginx/sites-available/api-staging.qalam.net.sa > /dev/null <<'EOF'
server {
    listen 80;
    server_name api-staging.qalam.net.sa;

    # Body size — file uploads (profile pics, teacher docs).
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

Issue the cert. Certbot will edit the vhost to add the 443 listener + redirect:

```sh
sudo certbot --nginx -d api-staging.qalam.net.sa --redirect --agree-tos -m you@example.com
```

Test renewal:

```sh
sudo certbot renew --dry-run
```

---

## 6. Smoke tests (run from your laptop)

```sh
# Health
curl https://api-staging.qalam.net.sa/health
# Expect: HTTP/2 200

# Login with the seeded admin
curl -sS -X POST https://api-staging.qalam.net.sa/Api/V1/Authentication/Login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@staging.qalam.net.sa","password":"YOUR_DEFAULT_ADMIN_PASSWORD"}' \
  | jq .

# Use the returned access token for an authenticated call
TOKEN='paste-access-token-here'
curl -sS https://api-staging.qalam.net.sa/Api/V1/Student/Courses \
  -H "Authorization: Bearer $TOKEN" | jq .
```

In SQL, confirm the unified schema landed:

```sh
sqlcmd -S 127.0.0.1 -U qalam_staging_user -P '...' -d qalam_staging -C -Q \
  "SELECT name FROM sys.tables WHERE name IN ('Enrollments','EnrollmentParticipants','EnrollmentPayments') ORDER BY name;"
# Expect three rows.
```

---

## 7. Rotate the demo admin password

The default-admin password is in `.env.staging`. Log in once with it, then rotate via the API (or directly in SQL). After that you can flip `SEED_DEFAULT_ADMIN=false` and `unset DEFAULT_ADMIN_PASSWORD` to remove the credential from disk — subsequent boots won't re-seed.

---

## 8. Verification checklist

- [ ] `dig +short api-staging.qalam.net.sa` returns `8.213.80.90`.
- [ ] `docker compose -p qalam-staging ps` shows 3 running services.
- [ ] `curl https://api-staging.qalam.net.sa/health` → 200.
- [ ] Admin login returns a JWT; authenticated call to `/Student/Courses` returns 200.
- [ ] Unified `Enrollments` / `EnrollmentParticipants` / `EnrollmentPayments` tables exist.
- [ ] `sudo certbot certificates` lists `api-staging.qalam.net.sa` with > 60 days remaining.
- [ ] `.env.staging` is `chmod 600` and owned by `deploy`.

Move on to [`04-production-setup.md`](./04-production-setup.md) once staging is green.