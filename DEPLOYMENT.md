# Deployment

One git clone on the VPS (e.g. `/opt/qalam-backend/Qalam`). **Staging** and **production** are separate Compose projects (different env files, project names, and ports).

| | Staging | Production |
|---|---------|------------|
| Compose file | `docker-compose.staging.yml` | `docker-compose.prod.yml` |
| Env file | `.env.staging` | `.env.prod` |
| Project name | `qalam-staging` | `qalam-prod` |
| Public URL | `https://api-staging.qalam.net.sa` | `https://api.qalam.net.sa` |
| API on host | `127.0.0.1:8081` | `127.0.0.1:8080` |
| Migrations | Auto (`MIGRATE_ON_STARTUP=true`) | Manual (`MIGRATE_ON_STARTUP=false`) |

---

## First-time setup (new VPS)

Follow **in order** — details in [`docs/deployment/`](./docs/deployment/README.md):

| Step | Guide |
|------|--------|
| 1 | [`01-sql-server-install.md`](./docs/deployment/01-sql-server-install.md) — SQL Server, backups |
| — | [`sql-bootstrap-qalam.sql`](./docs/deployment/sql-bootstrap-qalam.sql) — four DBs + logins |
| 2 | [`02-after-sql-bootstrap.md`](./docs/deployment/02-after-sql-bootstrap.md) — SQL → Docker → HTTPS |
| 3 | [`03-staging-setup.md`](./docs/deployment/03-staging-setup.md) — staging stack + Nginx |
| 4 | [`04-production-setup.md`](./docs/deployment/04-production-setup.md) — production stack + Nginx |
| 5 | [`05-nginx-subdomains.md`](./docs/deployment/05-nginx-subdomains.md) — frontend vhosts |

Before first build:

```bash
cd /opt/qalam-backend/Qalam
git clone <repo-url> .   # or git pull if already cloned

cp .env.staging.example .env.staging && chmod 600 .env.staging
# Fill DB, JWT, RABBITMQ, EMAIL_*, CORS, etc.

df -h /   # need free space (see “Disk space” below)
```

### First build — staging

```bash
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build
```

Watch until seeding finishes:

```bash
docker compose -p qalam-staging logs -f qalam-api
# Expect: Database migrations applied successfully / Database seeding completed successfully!

docker compose -p qalam-staging ps
curl -s http://127.0.0.1:8081/health
```

Then configure Nginx + TLS per [`03-staging-setup.md`](./docs/deployment/03-staging-setup.md).

### First build — production

Configure `.env.prod` first (`cp .env.prod.example .env.prod`). Production does **not** auto-migrate by default — see [`04-production-setup.md`](./docs/deployment/04-production-setup.md) for the first-boot migration gate (`MIGRATE_ON_STARTUP` / `SEED_DEFAULT_ADMIN`).

```bash
cd /opt/qalam-backend/Qalam
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --build
docker compose -p qalam-prod logs -f qalam-api
curl -s http://127.0.0.1:8080/health
```

Validate on staging before touching production.

---

## Updates (after code changes)

Always **staging first**, then production.

### 1. Free disk if needed

```bash
df -h /
docker system df
docker builder prune -af
docker system prune -af
```

If `/` is **100% full**, `git pull` and `--build` will fail. Docker build cache often uses most of a 40G disk.

### 2. Pull latest code

```bash
cd /opt/qalam-backend/Qalam
git fetch origin
git checkout main          # or: git fetch --tags && git checkout v0.x.x
git pull
```

### 3. Rebuild and restart

| Command | When |
|---------|------|
| `up -d --build` | **After `git pull`** — rebuilds images from Dockerfile, then starts containers |
| `up -d` | Config/env only, or image already current — **does not** rebuild code changes |
| `up -d` (no `--build`) | Old image may still run (Scalar, auth, email fixes won’t appear) |

**Staging (typical release):**

```bash
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build
docker compose -p qalam-staging logs -f qalam-api messaging-api
```

**Production** (after staging is green):

```bash
# Apply DB migrations manually if the release includes new migrations — see OPERATIONS_RUNBOOK §1
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod build qalam-api
# … run migration one-off if needed …
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --build
docker compose -p qalam-prod logs -f qalam-api messaging-api
```

Restart one service only (no rebuild):

```bash
docker compose -p qalam-staging restart qalam-api
docker compose -p qalam-prod restart messaging-api
```

### 4. Smoke test

```bash
# Staging
curl -s https://api-staging.qalam.net.sa/Api/V1/Authentication/Config | head -c 200
curl -sI https://api-staging.qalam.net.sa/scalar/v1

# Production
curl -s https://api.qalam.net.sa/health
```

OpenAPI (confirms deployed API version):

```bash
curl -s https://api-staging.qalam.net.sa/swagger/v1/swagger.json | grep -i "Authentication Config"
```

### 5. Prune after deploy (optional)

```bash
docker system prune -f
df -h /
```

---

## Env files (messaging / email)

SMTP and object storage env vars are wired on **messaging-api** only (see `.env.example`). After changing `EMAIL_*` or `WASABI_*`:

```bash
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build messaging-api
```

Main API (`qalam-api`) queues email to RabbitMQ; it does not need SMTP credentials when using the default **Queued** strategy.

---

## Related docs

| Doc | Purpose |
|-----|---------|
| [`docs/OPERATIONS_RUNBOOK.md`](./docs/OPERATIONS_RUNBOOK.md) | Migrations, backups, restore, lifecycle |
| [`docs/deployment/README.md`](./docs/deployment/README.md) | Full VPS setup index |
| [`docs/Auth-Config-Frontend.md`](./docs/Auth-Config-Frontend.md) | Auth config + email OTP flow |
| [`DOCKER_README.md`](./DOCKER_README.md) | Local Docker dev |
