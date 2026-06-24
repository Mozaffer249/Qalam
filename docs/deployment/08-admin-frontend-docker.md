# Admin frontend — Docker + Nginx verification

> **Prod URL:** `https://admin.qalam.net.sa`  
> **Container port:** `127.0.0.1:8090` → Next.js `:3000`  
> **API:** `https://api.qalam.net.sa` (baked at build via `NEXT_PUBLIC_API_URL`)

Same pattern as teacher (`8091` on `qalam.net.sa`). Admin uses subdomain **`admin.qalam.net.sa`**.

---

## 1. Clone admin repo (if missing)

```sh
sudo git clone https://github.com/yassin-khalid/qalam-admin.git /opt/qalam-admin
cd /opt/qalam-admin
git pull
```

Adjust the Git URL if yours differs.

---

## 2. Env for build (Next.js)

API URL is set at **build** time. Either:

**A — `.env.production` in the admin repo:**

```text
NEXT_PUBLIC_API_URL=https://api.qalam.net.sa
```

**B — build arg via backend compose** (from `/opt/qalam-backend/Qalam/.env`):

```text
ADMIN_APP_PATH=/opt/qalam-admin
ADMIN_API_URL=https://api.qalam.net.sa
```

---

## 3. Build & run container

**Standalone (recommended for first verify):**

```sh
cd /opt/qalam-admin
docker build -t qalam-admin:latest \
  --build-arg NEXT_PUBLIC_API_URL=https://api.qalam.net.sa \
  .

docker rm -f qalam-admin 2>/dev/null
docker run -d --name qalam-admin --restart unless-stopped \
  -p 127.0.0.1:8090:3000 \
  qalam-admin:latest
```

**Or via backend compose:**

```sh
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.yml --env-file .env up -d --build qalam-admin
```

---

## 4. Local smoke test

```sh
curl -I http://127.0.0.1:8090/
docker ps | grep qalam-admin
docker logs --tail 30 qalam-admin
```

Expect **HTTP 200**.

---

## 5. Nginx → `admin.qalam.net.sa`

```sh
sudo tee /etc/nginx/sites-available/admin.qalam.net.sa > /dev/null <<'EOF'
server {
    listen 80;
    server_name admin.qalam.net.sa;

    client_max_body_size 25M;

    location / {
        proxy_pass http://127.0.0.1:8090;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 90s;
    }
}
EOF

sudo ln -sf /etc/nginx/sites-available/admin.qalam.net.sa /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/qalam-admin.conf   # old static-site vhost if present
sudo nginx -t && sudo systemctl reload nginx
```

HTTPS:

```sh
sudo certbot --nginx -d admin.qalam.net.sa --redirect
```

---

## 6. Backend CORS

In `/opt/qalam-backend/Qalam/.env`:

```text
CORS_ALLOWED_ORIGINS=https://qalam.net.sa,https://admin.qalam.net.sa
```

Restart API:

```sh
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.yml --env-file .env up -d --force-recreate qalam-api
```

---

## 7. End-to-end checks

```sh
curl -I https://admin.qalam.net.sa/
curl -sS https://api.qalam.net.sa/health
```

Browser: open `https://admin.qalam.net.sa` → login with prod admin (from `.env` `DEFAULT_ADMIN_EMAIL` / password you set, or seeded admin).

API login test (correct admin endpoint):

```sh
curl -sS -X POST https://api.qalam.net.sa/Api/V1/Authentication/Admin/Login \
  -H 'Content-Type: application/json' \
  -d '{"userNameOrEmail":"admin@qalam.net.sa","password":"YOUR_ADMIN_PASSWORD"}'
```

Success looks like:

```json
{
  "succeeded": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": { "tokenString": "...", "expireAt": "..." },
    "userName": "...",
    "email": "..."
  }
}
```

If `succeeded` is **false**, `data` is **null** — the admin app must not read `data.accessToken` without checking `succeeded` first.

Wrong endpoint (404 / no token): `/Api/V1/Authentication/Login` — use **`/Admin/Login`** only.

---

## 8. Checklist

- [ ] `curl http://127.0.0.1:8090/` → 200
- [ ] `curl https://admin.qalam.net.sa/` → 200
- [ ] `https://api.qalam.net.sa/health` → 200
- [ ] Admin login works; no CORS errors in browser DevTools
- [ ] `NEXT_PUBLIC_API_URL` points to `https://api.qalam.net.sa` (rebuild if wrong)

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| 502 on `admin.qalam.net.sa` | `docker ps` — start `qalam-admin`; confirm `8090` |
| Wrong site / static old admin | Remove `/var/www/qalam-admin` vhost; use proxy to `8090` |
| API calls to `localhost` | Rebuild with `NEXT_PUBLIC_API_URL=https://api.qalam.net.sa` |
| CORS error in browser | Add `https://admin.qalam.net.sa` to `CORS_ALLOWED_ORIGINS`, recreate API |
| Container name conflict | `docker rm -f qalam-admin` then `docker run` again |

**Rebuild after code/env change:**

```sh
cd /opt/qalam-admin
docker build --no-cache -t qalam-admin:latest --build-arg NEXT_PUBLIC_API_URL=https://api.qalam.net.sa .
docker rm -f qalam-admin
docker run -d --name qalam-admin --restart unless-stopped -p 127.0.0.1:8090:3000 qalam-admin:latest
```
