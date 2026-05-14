# Staging Setup Guide

Brings up the **staging** docker-compose stack at `https://api-staging.qalam.net.sa`. Runs side-by-side with the production stack on the same VPS, with its own databases, RabbitMQ broker, and Nginx vhost.

Prerequisites: [`SQL_SERVER_INSTALL.md`](./SQL_SERVER_INSTALL.md) is complete and the `qalam_staging` + `qalam_messaging_staging` databases plus `qalam_staging_user` login already exist.

---

## 1. DNS record

Add **one** A record at your DNS host:

```
api-staging.qalam.net.sa.   A   8.213.80.90   300
```

Confirm propagation from your laptop:

```bash
dig +short api-staging.qalam.net.sa
# Expect: 8.213.80.90
```

Wait for TTL before continuing — Certbot's HTTP-01 challenge needs DNS resolution.

---

## 2. Clone the repo

Use a non-root deploy user (create one if you don't have one yet):

```bash
sudo adduser --disabled-password --gecos "" deploy
sudo usermod -aG docker deploy
sudo -iu deploy
```

Then as `deploy`:

```bash
sudo mkdir -p /opt/qalam-staging
sudo chown deploy:deploy /opt/qalam-staging
git clone https://github.com/Mozaffer249/Qalam.git /opt/qalam-staging
cd /opt/qalam-staging
git checkout main          # or the release tag you want to ship to staging
```

---

## 3. Configure `.env.staging`

```bash
cp .env.staging.example .env.staging
chmod 600 .env.staging
nano .env.staging
```

Required fills (everything else in the template has sensible defaults):

| Variable | Value |
|----------|-------|
| `DB_CONNECTION_STRING` | Replace the `REPLACE_ME` password with the staging SQL password you generated during install |
| `MESSAGING_DB_CONNECTION_STRING` | Same password, different DB name (already correct in the template) |
| `ENCRYPTION_KEY` | `openssl rand -hex 16` |
| `JWT_SECRET` | `openssl rand -base64 48` |
| `RABBITMQ_PASS` | `openssl rand -base64 24` |
| `DEFAULT_ADMIN_PASSWORD` | Strong password (you'll log in with this once, then rotate via the API) |
| `EMAIL_FROM_EMAIL`, `EMAIL_USERNAME`, `EMAIL_PASSWORD` | SMTP creds (Mailtrap sandbox is fine for staging) |

Verify the file is locked down:

```bash
ls -la .env.staging
# expect: -rw------- 1 deploy deploy ... .env.staging
```

---

## 4. Bring up the stack

```bash
cd /opt/qalam-staging
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build
```

First boot does three things, in this order:

1. RabbitMQ container starts.
2. MessagingApi connects to RabbitMQ + `qalam_messaging_staging`, runs migrations on that DB.
3. Qalam API connects to `qalam_staging`, runs migrations (`MIGRATE_ON_STARTUP=true` for staging), seeds reference data + roles + the demo admin (`SEED_DEFAULT_ADMIN=true`).

Watch logs until you see `Database seeding completed successfully!`:

```bash
docker compose -f docker-compose.staging.yml -p qalam-staging logs -f qalam-api
```

Confirm three containers are up:

```bash
docker compose -p qalam-staging ps
# Expect: qalam-staging-rabbitmq, qalam-staging-messaging-api, qalam-staging-backend-api — all "running".
```

Quick local check (still on the VPS):

```bash
curl http://127.0.0.1:8081/health
# Expect: 200 OK with health payload
```

---

## 5. Nginx vhost + TLS cert

Install Nginx and Certbot if not already on the box:

```bash
sudo apt install -y nginx certbot python3-certbot-nginx
```

Create the staging vhost:

```bash
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

```bash
sudo certbot --nginx -d api-staging.qalam.net.sa --redirect --agree-tos -m you@example.com
```

Test renewal:

```bash
sudo certbot renew --dry-run
```

---

## 6. Smoke tests (run from your laptop)

```bash
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

```bash
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

Move on to [`PRODUCTION_SETUP.md`](./PRODUCTION_SETUP.md) once staging is green.
