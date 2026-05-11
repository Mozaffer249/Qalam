# Deploy API + Web Apps on One Server (Nginx + HTTPS + Subdomains)

This guide assumes:

- One Linux VPS (e.g. Alibaba ECS) with public IP **8.213.80.90** (replace with yours).
- Domain **qalam.net.sa** (replace with yours).
- Backend runs in Docker (`docker-compose.prod.yml`) and listens on the host at **`127.0.0.1:8080`** only (recommended).
- You will serve **four** sites on the same machine:

| Subdomain | Purpose | Typical Nginx target |
|-----------|---------|----------------------|
| `api.qalam.net.sa` | ASP.NET Core API (Swagger + REST) | Reverse proxy → `http://127.0.0.1:8080` |
| `www.qalam.net.sa` | Public / student web app | Static files **or** reverse proxy to a container |
| `teacher.qalam.net.sa` | Teacher dashboard | Static files **or** reverse proxy to a container |
| `admin.qalam.net.sa` | Admin dashboard | Static files **or** reverse proxy to a container |

You can change subdomain names; keep **one subdomain for the API** so HTTPS and CORS stay simple.

---

## Step 1 — DNS (at your domain registrar)

Create **A records** (all pointing to your server public IP):

| Type | Host / Name | Value |
|------|-------------|--------|
| A | `api` | `8.213.80.90` |
| A | `www` | `8.213.80.90` |
| A | `teacher` | `8.213.80.90` |
| A | `admin` | `8.213.80.90` |

Optional but useful:

| Type | Host | Value |
|------|------|--------|
| A | `@` | `8.213.80.90` |

Wait until DNS resolves from the server:

```bash
dig +short api.qalam.net.sa
dig +short www.qalam.net.sa
```

They should return your server IP before you run Let’s Encrypt.

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

For each SPA (www / teacher / admin), produce a production build (`npm run build` or equivalent) and copy the output folder to the server, for example:

```text
/var/www/qalam-www/html
/var/www/qalam-teacher/html
/var/www/qalam-admin/html
```

Create directories and set ownership (adjust user/group if needed):

```bash
sudo mkdir -p /var/www/qalam-{www,teacher,admin}/html
sudo chown -R www-data:www-data /var/www/qalam-www /var/www/qalam-teacher /var/www/qalam-admin
```

Copy your built `index.html` and assets into each `html` folder.

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

## Step 6 — Configure `CORS_ALLOWED_ORIGINS` (API `.env`)

The API must allow browser calls from your HTTPS frontends. In `.env` on the server, set **comma-separated** origins (no spaces), for example:

```env
CORS_ALLOWED_ORIGINS=https://www.qalam.net.sa,https://teacher.qalam.net.sa,https://admin.qalam.net.sa
```

If you still use Vercel during migration, append it:

```env
CORS_ALLOWED_ORIGINS=https://www.qalam.net.sa,https://teacher.qalam.net.sa,https://admin.qalam.net.sa,https://your-app.vercel.app
```

Apply changes:

```bash
docker compose -f docker-compose.prod.yml up -d
```

---

## Step 7 — Nginx: API reverse proxy

Create a site file (Ubuntu/Debian style):

```bash
sudo nano /etc/nginx/sites-available/qalam-api.conf
```

Paste (replace domain):

```nginx
server {
    listen 80;
    server_name api.qalam.net.sa;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable and test:

```bash
sudo ln -sf /etc/nginx/sites-available/qalam-api.conf /etc/nginx/sites-enabled/qalam-api.conf
sudo nginx -t && sudo systemctl reload nginx
```

---

## Step 8 — Nginx: static sites (www / teacher / admin)

Example for **www** (repeat with different `server_name` and `root` for teacher and admin):

```bash
sudo nano /etc/nginx/sites-available/qalam-www.conf
```

```nginx
server {
    listen 80;
    server_name www.qalam.net.sa;

    root /var/www/qalam-www/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

Enable:

```bash
sudo ln -sf /etc/nginx/sites-available/qalam-www.conf /etc/nginx/sites-enabled/qalam-www.conf
```

Repeat files:

- `qalam-teacher.conf` → `server_name teacher.qalam.net.sa;` → `root /var/www/qalam-teacher/html;`
- `qalam-admin.conf` → `server_name admin.qalam.net.sa;` → `root /var/www/qalam-admin/html;`

Then:

```bash
sudo nginx -t && sudo systemctl reload nginx
```

---

## Step 9 — Issue HTTPS certificates (Let’s Encrypt)

After DNS resolves correctly:

```bash
sudo certbot --nginx \
  -d api.qalam.net.sa \
  -d www.qalam.net.sa \
  -d teacher.qalam.net.sa \
  -d admin.qalam.net.sa
```

Follow the prompts. Certbot will modify Nginx configs for TLS and optional HTTP→HTTPS redirect.

Test renewal:

```bash
sudo certbot renew --dry-run
```

---

## Step 10 — Point frontends to the API

In each frontend’s environment (build-time or runtime config), set the API base URL to:

```text
https://api.qalam.net.sa
```

Rebuild and redeploy the static files to `/var/www/...` after changes.

---

## Step 11 — Smoke tests

From your laptop:

```bash
curl -I https://api.qalam.net.sa/swagger/index.html
curl -I https://www.qalam.net.sa/
curl -I https://teacher.qalam.net.sa/
curl -I https://admin.qalam.net.sa/
```

From browser: open Swagger at `https://api.qalam.net.sa/swagger` and confirm no mixed-content errors when the SPA calls the API.

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

- Production Compose: [`docker-compose.prod.yml`](docker-compose.prod.yml) (API bound to `127.0.0.1:8080`).
- Environment template: [`.env.example`](.env.example) (`CORS_ALLOWED_ORIGINS`, `DB_CONNECTION_STRING`, etc.).
- General VPS notes: [`DEPLOYMENT.md`](DEPLOYMENT.md).

---

## Security checklist (short)

- Do not commit `.env` to git.
- Prefer SSH key login; restrict port 22 by IP if possible.
- Keep only **80/443** public for web; keep **8080** localhost-only behind Nginx.
- Use strong `JWT_SECRET`, `RABBITMQ_PASS`, and database passwords.
