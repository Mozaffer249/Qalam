# Deploy API + Web Apps on One Server (Nginx + HTTPS + Subdomains)

> **The API vhosts in this guide are now authoritative-documented in [`docs/STAGING_SETUP.md`](./docs/STAGING_SETUP.md) and [`docs/PRODUCTION_SETUP.md`](./docs/PRODUCTION_SETUP.md).** Use those for the API. This file remains the reference for **frontend SPA** vhosts (student / teacher / admin) on the same VPS, plus the consolidated cert-issuance step.

This guide assumes:

- One Linux VPS with public IP **8.213.80.90** (replace with yours).
- Domain **qalam.net.sa** (replace with yours).
- Backend runs in Docker (one stack per environment — see compose files) and listens on the host at **`127.0.0.1:8080`** (prod) / **`127.0.0.1:8081`** (staging).
- You will serve **eight** vhosts on the same machine — one set per environment:

**Production**

| Subdomain | Purpose | Typical Nginx target |
|-----------|---------|----------------------|
| `api.qalam.net.sa` | ASP.NET Core API (prod) | Reverse proxy → `http://127.0.0.1:8080` |
| `qalam.net.sa` | Public / student web app (prod) | Static files **or** reverse proxy to a container |
| `teacher.qalam.net.sa` | Teacher dashboard (prod) | Static files **or** reverse proxy to a container |
| `admin.qalam.net.sa` | Admin dashboard (prod) | Static files **or** reverse proxy to a container |

**Staging**

| Subdomain | Purpose | Typical Nginx target |
|-----------|---------|----------------------|
| `api-staging.qalam.net.sa` | ASP.NET Core API (staging) | Reverse proxy → `http://127.0.0.1:8081` |
| `teacher-staging.qalam.net.sa` | Teacher dashboard (staging) | Static files **or** reverse proxy to a container |
| `admin-staging.qalam.net.sa` | Admin dashboard (staging) | Static files **or** reverse proxy to a container |

The public landing page at `qalam.net.sa` intentionally has **no staging vhost** — it's mostly static marketing content, low-risk to change, and faster to preview via the build tool's PR-preview feature (Vercel/Netlify). Add `staging.qalam.net.sa` later only if the landing grows real API-driven flows (sign-up, course catalog with live data, etc.).

Keep **one subdomain per environment for the API** so HTTPS and CORS stay simple.

---

## Step 1 — DNS (at your domain registrar)

**Production records — already in place** (confirmed live):

| Type | Host / Name | Value |
|------|-------------|--------|
| A | `@` (root `qalam.net.sa`) | `8.213.80.90` |
| A | `api` | `8.213.80.90` |
| A | `teacher` | `8.213.80.90` |
| A | `admin` | `8.213.80.90` |

**Staging records — add these** before running the staging setup or issuing certs:

| Type | Host / Name | Value |
|------|-------------|--------|
| A | `api-staging` | `8.213.80.90` |
| A | `teacher-staging` | `8.213.80.90` |
| A | `admin-staging` | `8.213.80.90` |

Wait until DNS resolves before continuing:

```bash
dig +short api.qalam.net.sa            # prod
dig +short qalam.net.sa                # prod landing page
dig +short api-staging.qalam.net.sa    # staging API
dig +short teacher-staging.qalam.net.sa
```

All should return `8.213.80.90` before you run Let’s Encrypt.

---

## Step 2 — Cloud firewall (Alibaba Security Group)

Allow inbound:

- **TCP 22** — SSH (restrict to your IP if possible).
- **TCP 80** — HTTP (for Let’s Encrypt HTTP-01 and redirects).
- **TCP 443** — HTTPS.

You usually **do not** need to expose **8080** publicly if Nginx proxies to `127.0.0.1:8080`.

---

## Step 3 — Install Nginx and Certbot

SSH into the server:

```bash
sudo apt update
sudo apt install -y nginx certbot python3-certbot-nginx
sudo systemctl enable --now nginx
```

If `ufw` is enabled:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
```

---

## Step 4 — Build and deploy frontends on the server

For each SPA produce a production build (`npm run build`) and copy the output folder to the server. The student landing page only has a prod build; teacher and admin dashboards have both prod and staging.

```text
# Production
/var/www/qalam-student/html            # serves qalam.net.sa  (landing — prod only)
/var/www/qalam-teacher/html            # serves teacher.qalam.net.sa
/var/www/qalam-admin/html              # serves admin.qalam.net.sa

# Staging
/var/www/qalam-teacher-staging/html    # serves teacher-staging.qalam.net.sa
/var/www/qalam-admin-staging/html      # serves admin-staging.qalam.net.sa
```

Create directories and set ownership:

```bash
sudo mkdir -p /var/www/qalam-{student,teacher,admin}/html
sudo mkdir -p /var/www/qalam-{teacher,admin}-staging/html
sudo chown -R www-data:www-data /var/www/qalam-*
```

Copy each built `index.html` + assets bundle into the matching `html` folder. Staging builds (teacher/admin) should point at `https://api-staging.qalam.net.sa`; prod builds at `https://api.qalam.net.sa`.

**Note:** If you prefer to run each frontend in Docker on `127.0.0.1:3001`, `3002`, etc., replace `root` + `try_files` in the Nginx blocks below with `proxy_pass` to those ports instead.

---

## Step 5 — Start the API with Docker (production compose)

From your project directory (where `docker-compose.prod.yml` and `.env` live):

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

Confirm the API responds locally:

```bash
curl -sS -o /dev/null -w "%{http_code}\n" http://127.0.0.1:8080/swagger/index.html
```

You should see **200** (GET). A **HEAD** request may return **404** for Swagger; that is normal.

---

## Step 6 — Configure `CORS_ALLOWED_ORIGINS` (per environment)

Each environment's API must allow browser calls only from its own frontends. Set **comma-separated** origins (no spaces).

**Production** — in `/opt/qalam-prod/.env.prod`:

```env
CORS_ALLOWED_ORIGINS=https://qalam.net.sa,https://teacher.qalam.net.sa,https://admin.qalam.net.sa
```

**Staging** — in `/opt/qalam-staging/.env.staging` (no landing-page origin — landing is prod-only):

```env
CORS_ALLOWED_ORIGINS=https://teacher-staging.qalam.net.sa,https://admin-staging.qalam.net.sa
```

If you still use Vercel during migration, append it to the relevant env file.

Apply changes:

```bash
# Prod
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d

# Staging
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d
```

---

## Step 7 — Nginx: API reverse proxies

> Authoritative source: [`docs/PRODUCTION_SETUP.md`](./docs/PRODUCTION_SETUP.md) §5 and [`docs/STAGING_SETUP.md`](./docs/STAGING_SETUP.md) §5. The vhost blocks below are reproduced here only for completeness when issuing certs in Step 9.

**Production API vhost** (`/etc/nginx/sites-available/api.qalam.net.sa`):

```nginx
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
```

**Staging API vhost** (`/etc/nginx/sites-available/api-staging.qalam.net.sa`):

```nginx
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
```

Enable both and test:

```bash
sudo ln -sf /etc/nginx/sites-available/api.qalam.net.sa         /etc/nginx/sites-enabled/api.qalam.net.sa
sudo ln -sf /etc/nginx/sites-available/api-staging.qalam.net.sa /etc/nginx/sites-enabled/api-staging.qalam.net.sa
sudo nginx -t && sudo systemctl reload nginx
```

---

## Step 8 — Nginx: static sites (student / teacher / admin × prod / staging)

Pattern (use the same block for every SPA — just swap `server_name` and `root`):

```nginx
server {
    listen 80;
    server_name SERVER_NAME_HERE;

    root /var/www/SITE_DIRECTORY/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

**Production** — create six? no, three vhost files:

- `qalam-student.conf` → `server_name qalam.net.sa;`           → `root /var/www/qalam-student/html;`
- `qalam-teacher.conf` → `server_name teacher.qalam.net.sa;`   → `root /var/www/qalam-teacher/html;`
- `qalam-admin.conf`   → `server_name admin.qalam.net.sa;`     → `root /var/www/qalam-admin/html;`

**Staging** — two more vhost files (no staging landing page):

- `qalam-teacher-staging.conf` → `server_name teacher-staging.qalam.net.sa;` → `root /var/www/qalam-teacher-staging/html;`
- `qalam-admin-staging.conf`   → `server_name admin-staging.qalam.net.sa;`   → `root /var/www/qalam-admin-staging/html;`

Enable everything:

```bash
for f in qalam-student qalam-teacher qalam-admin \
         qalam-teacher-staging qalam-admin-staging; do
  sudo ln -sf /etc/nginx/sites-available/${f}.conf /etc/nginx/sites-enabled/${f}.conf
done
sudo nginx -t && sudo systemctl reload nginx
```

---

## Step 9 — Issue HTTPS certificates (Let’s Encrypt)

After DNS resolves correctly for **all** subdomains (Step 1), issue certs in two batches — one per environment, so the cert for staging can renew independently:

```bash
# Production cert (4 hostnames)
sudo certbot --nginx \
  -d api.qalam.net.sa \
  -d qalam.net.sa \
  -d teacher.qalam.net.sa \
  -d admin.qalam.net.sa \
  --redirect --agree-tos -m you@example.com

# Staging cert (3 hostnames — no landing page)
sudo certbot --nginx \
  -d api-staging.qalam.net.sa \
  -d teacher-staging.qalam.net.sa \
  -d admin-staging.qalam.net.sa \
  --redirect --agree-tos -m you@example.com
```

Follow the prompts. Certbot will modify the matching Nginx vhosts for TLS and add the HTTP→HTTPS redirect.

Test renewal (both certs at once):

```bash
sudo certbot renew --dry-run
sudo certbot certificates    # confirm both certs are listed with > 60 days remaining
```

---

## Step 10 — Point frontends to the API

In each frontend's environment (build-time or runtime config), set the API base URL **per environment**:

```text
# Production builds (qalam.net.sa, teacher.qalam.net.sa, admin.qalam.net.sa)
VITE_API_URL=https://api.qalam.net.sa

# Staging builds (staging.qalam.net.sa, teacher-staging.qalam.net.sa, admin-staging.qalam.net.sa)
VITE_API_URL=https://api-staging.qalam.net.sa
```

Rebuild each environment separately and deploy to the matching directory under `/var/www/`. Don't reuse a staging build under a prod hostname — the embedded API URL would silently misroute traffic.

---

## Step 11 — Smoke tests

From your laptop:

```bash
# Production
curl -I https://api.qalam.net.sa/swagger/index.html
curl -I https://qalam.net.sa/
curl -I https://teacher.qalam.net.sa/
curl -I https://admin.qalam.net.sa/

# Staging (no landing page)
curl -I https://api-staging.qalam.net.sa/swagger/index.html
curl -I https://teacher-staging.qalam.net.sa/
curl -I https://admin-staging.qalam.net.sa/
```

All should return `HTTP/2 200` (or `301` for the HTTP redirect followed by `200` on HTTPS). From the browser, open `https://api.qalam.net.sa/swagger` and `https://api-staging.qalam.net.sa/swagger`; confirm no mixed-content errors when each SPA calls its matching API.

---

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| `502 Bad Gateway` on `api.*` | API container down or not listening on `127.0.0.1:8080` — run `docker compose ... ps` and `curl http://127.0.0.1:8080/swagger/index.html` on the server. |
| CORS errors in browser | `CORS_ALLOWED_ORIGINS` missing the exact frontend origin (scheme + host, no trailing slash). |
| Certbot fails | DNS not propagated, or ports 80/443 blocked, or wrong `server_name`. |
| SPA routes 404 on refresh | Missing `try_files $uri $uri/ /index.html;` in the static site block. |

---

## Related project files

- Production Compose: [`docker-compose.prod.yml`](docker-compose.prod.yml) — API bound to `127.0.0.1:8080`.
- Staging Compose: [`docker-compose.staging.yml`](docker-compose.staging.yml) — API bound to `127.0.0.1:8081`.
- Environment templates:
  - [`.env.prod.example`](.env.prod.example) — locked-down prod template.
  - [`.env.staging.example`](.env.staging.example) — permissive staging template.
- Authoritative API setup guides:
  - [`docs/PRODUCTION_SETUP.md`](docs/PRODUCTION_SETUP.md)
  - [`docs/STAGING_SETUP.md`](docs/STAGING_SETUP.md)
- Database install: [`docs/SQL_SERVER_INSTALL.md`](docs/SQL_SERVER_INSTALL.md).
- Day-2 ops: [`docs/OPERATIONS_RUNBOOK.md`](docs/OPERATIONS_RUNBOOK.md).
- One-page deployment index: [`DEPLOYMENT.md`](DEPLOYMENT.md).

---

## Security checklist (short)

- Do not commit `.env` to git.
- Prefer SSH key login; restrict port 22 by IP if possible.
- Keep only **80/443** public for web; keep **8080** localhost-only behind Nginx.
- Use strong `JWT_SECRET`, `RABBITMQ_PASS`, and database passwords.
