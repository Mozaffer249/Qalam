# Student frontend — Flutter web on Docker + Nginx

> **Prod URL:** `https://student.qalam.net.sa` → container `127.0.0.1:8095`
> **Staging URL:** `https://student-staging.qalam.net.sa` → container `127.0.0.1:8094`
> **API:** compiled into the bundle via the Flutter flavor (`main_production.dart` → `https://api.qalam.net.sa`, `main_staging.dart` → `https://api-staging.qalam.net.sa`).

Unlike teacher/admin (Node/SSR containers), the student app is **Flutter web**: it builds to a static bundle (`build/web/`) that is served by `nginx:alpine` inside the container. The API URL is **baked in at build time** from [`lib/core/config/flavor_config.dart`](../../apps/Qalam/lib/core/config/flavor_config.dart) — there is no runtime env var.

Source lives in-repo at [`apps/Qalam`](../../apps/Qalam). Build files:

- [`apps/Qalam/Dockerfile`](../../apps/Qalam/Dockerfile) — multi-stage (Flutter build → nginx serve), `FLAVOR` build arg
- [`apps/Qalam/nginx.conf`](../../apps/Qalam/nginx.conf) — SPA fallback for `go_router` deep links

---

## 1. Flavor / API URL

Confirm the compiled-in API URLs before deploying:

- Staging → `https://api-staging.qalam.net.sa`
- Production → `https://api.qalam.net.sa`

Both are set in [`flavor_config.dart`](../../apps/Qalam/lib/core/config/flavor_config.dart). Rebuild the image after any change — the URL is embedded in the bundle.

---

## 2. Build & run container

Both environments build from the same compose files as the rest of the stack.

**Staging:**

```sh
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging \
  up -d --build qalam-student
```

**Production:**

```sh
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.yml -p qalam-prod --env-file .env \
  up -d --build qalam-student
```

> First build pulls the Flutter SDK image and compiles the web bundle — expect ~5-10 min on a small VPS. Subsequent builds are faster if `pubspec.*` is unchanged (pub deps are cached in an early layer).

---

## 3. Local smoke test (on the VPS)

```sh
curl -I http://127.0.0.1:8094/     # staging
curl -I http://127.0.0.1:8095/     # production
docker ps | grep qalam-student
docker logs --tail 30 qalam-staging-student
```

Expect **HTTP 200** serving `index.html`.

---

## 4. DNS

Add A records at the registrar (both point at the VPS):

| Type | Host | Value |
|------|------|-------|
| A | `student-staging` | `8.213.80.90` |
| A | `student` | `8.213.80.90` |

Wait for resolution:

```sh
dig +short student-staging.qalam.net.sa
dig +short student.qalam.net.sa
```

---

## 5. Nginx vhosts

**Staging** → `student-staging.qalam.net.sa`:

```sh
sudo tee /etc/nginx/sites-available/student-staging.qalam.net.sa > /dev/null <<'EOF'
server {
    listen 80;
    server_name student-staging.qalam.net.sa;

    location / {
        proxy_pass http://127.0.0.1:8094;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 90s;
    }
}
EOF

sudo ln -sf /etc/nginx/sites-available/student-staging.qalam.net.sa /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

**Production** → `student.qalam.net.sa`: same block, `server_name student.qalam.net.sa;` and `proxy_pass http://127.0.0.1:8095;`.

---

## 6. HTTPS (extend existing cert batches)

Add the student hosts to the matching Certbot batch (see [`05-nginx-subdomains.md`](./05-nginx-subdomains.md) §9):

```sh
# Staging cert (add -d student-staging)
sudo certbot --nginx \
  -d api-staging.qalam.net.sa \
  -d teacher-staging.qalam.net.sa \
  -d admin-staging.qalam.net.sa \
  -d student-staging.qalam.net.sa \
  --redirect --agree-tos -m info@qalam.net.sa

# Production cert (add -d student)
sudo certbot --nginx \
  -d api.qalam.net.sa \
  -d qalam.net.sa \
  -d admin.qalam.net.sa \
  -d student.qalam.net.sa \
  --redirect --agree-tos -m info@qalam.net.sa
```

---

## 7. Backend CORS

The API only allows browser calls from whitelisted origins. Add the student hosts to `CORS_ALLOWED_ORIGINS`.

**Staging** — `/opt/qalam-backend/Qalam/.env.staging`:

```text
CORS_ALLOWED_ORIGINS=...,https://student-staging.qalam.net.sa
```

**Production** — `/opt/qalam-backend/Qalam/.env`:

```text
CORS_ALLOWED_ORIGINS=https://qalam.net.sa,https://admin.qalam.net.sa,https://student.qalam.net.sa
```

Recreate the API so the new origin takes effect:

```sh
# staging
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging \
  up -d --force-recreate qalam-api
# prod
docker compose -f docker-compose.yml -p qalam-prod --env-file .env \
  up -d --force-recreate qalam-api
```

---

## 8. End-to-end checks

```sh
curl -I https://student-staging.qalam.net.sa/
curl -I https://student.qalam.net.sa/
```

Browser:

- Open each URL, confirm the app loads and login works against the matching API.
- Refresh on a routed page (e.g. after navigating) — nginx SPA fallback must serve `index.html`, not 404.
- Check DevTools → Network for CORS errors against `api(-staging).qalam.net.sa`.

---

## 9. Checklist

- [ ] `curl http://127.0.0.1:8094/` and `:8095/` → 200
- [ ] `curl https://student-staging.qalam.net.sa/` and `https://student.qalam.net.sa/` → 200
- [ ] Compiled API URL matches the environment (rebuild if wrong)
- [ ] Login works; no CORS errors in browser DevTools
- [ ] Deep-link refresh serves the app (SPA fallback)

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| 502 on the subdomain | `docker ps` — start `qalam-student`; confirm port `8094`/`8095` |
| 404 on refresh of a route | Ensure `nginx.conf` `try_files … /index.html` is in the image |
| API calls to wrong host | Rebuild image — API URL is compiled from `flavor_config.dart` |
| CORS error in browser | Add the student origin to `CORS_ALLOWED_ORIGINS`, recreate API |
| Slow/stuck first build | Normal — Flutter SDK image + web compile; watch `docker logs` |

**Rebuild after code change:**

```sh
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging \
  up -d --build qalam-student        # or the prod compose/env
```
