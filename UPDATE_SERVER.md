# Update the server after a code change

Day-to-day cheat sheet for pushing new code from this repo to the staging and production VPS stacks. Both stacks run from **one** clone on the VPS (e.g. `/opt/qalam-backend/Qalam`) with separate `.env.*` files and Compose project names.

| | Staging | Production |
|---|---|---|
| Compose file | `docker-compose.staging.yml` | `docker-compose.prod.yml` |
| Compose project | `qalam-staging` | `qalam-prod` |
| Env file | `.env.staging` | `.env.prod` |
| `MIGRATE_ON_STARTUP` | `true` — migrations run automatically on boot | `false` — migrations are applied manually in a window |
| API container | `qalam-staging-backend-api` | `qalam-backend-api` |
| Messaging container | `qalam-staging-messaging-api` | `qalam-messaging-api` |
| RabbitMQ container | `qalam-staging-rabbitmq` | `qalam-rabbitmq` |
| URL | `https://api-staging.qalam.net.sa` | `https://api.qalam.net.sa` |

For first-time / fresh setup see [DEPLOYMENT.md](./DEPLOYMENT.md). For incidents / backups see [docs/OPERATIONS_RUNBOOK.md](./docs/OPERATIONS_RUNBOOK.md).

---

## 1. Push your code from your laptop

```bash
# From your local repo
git push origin main          # or your release branch
# Optional but recommended for production:
git tag v0.x.y && git push origin v0.x.y
```

---

## 2. Update staging

Staging is the safe path — it auto-migrates and seeds, so one command is enough.

```bash
ssh deploy@<your-vps>
cd /opt/qalam-backend/Qalam

# Pull the new code
git fetch --all
git checkout main           # or: git checkout v0.x.y
git pull --ff-only

# Rebuild + restart the stack (zero-downtime per service)
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build

# Watch the API come up — wait for "Database seeding completed successfully!" or the migration log line
docker compose -f docker-compose.staging.yml -p qalam-staging logs -f qalam-api
```

If only one service changed (e.g. messaging only), you can rebuild just that:

```bash
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build messaging-api
```

### Verify staging

```bash
# Containers running?
docker compose -p qalam-staging ps
# Expect: qalam-staging-rabbitmq, qalam-staging-messaging-api, qalam-staging-backend-api — all "running"

# Health check (still on the VPS)
curl http://127.0.0.1:8081/health
# Expect: 200 OK

# Health check (from your laptop)
curl https://api-staging.qalam.net.sa/health
```

---

## 3. Update production (canonical release flow)

Production has `MIGRATE_ON_STARTUP=false` — schema changes are applied explicitly so a bad migration cannot wedge the running stack.

**Step A — promote the same git ref staging just validated.**

```bash
ssh deploy@<your-vps>
cd /opt/qalam-backend/Qalam
git fetch --tags
git checkout v0.x.y          # use the tag you already verified on staging
```

**Step B — build the new image without starting it.**

```bash
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod build qalam-api messaging-api
```

**Step C — apply migrations via a throwaway container (only if this release contains EF Core migrations).**

```bash
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod \
  run --rm \
  -e MIGRATE_ON_STARTUP=true \
  -e SEED_DEFAULT_ADMIN=false \
  qalam-api bash -lc '
    dotnet Qalam.Api.dll &
    APP_PID=$!
    while ! grep -q "Database migrations applied successfully" /app/Logs/log-*.txt 2>/dev/null; do sleep 2; done
    kill $APP_PID || true
  '
```

If this exits non-zero, **production stays on the old image** — fix the migration locally, push a new tag, redo from step A.

**Step D — bring the long-lived prod stack up on the new image.**

```bash
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d
docker compose -p qalam-prod logs -f qalam-api
# Expect: "Skipping auto-migration in Production (MIGRATE_ON_STARTUP=false)"
```

### Verify production

```bash
docker compose -p qalam-prod ps                                  # 3 running services
curl http://127.0.0.1:8080/health                                # 200 OK on VPS
curl https://api.qalam.net.sa/health                             # 200 OK from outside
```

---

## 4. Code-only changes (no migration, no Dockerfile change)

If your change is purely C# code and you don't want a full image rebuild, you can rebuild only:

```bash
# Staging
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build qalam-api

# Production (still need to git pull / checkout first)
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --build qalam-api
```

The Dockerfile uses a multi-stage build with NuGet layer caching, so subsequent rebuilds typically take 30–90 seconds.

---

## 5. Rollback

Roll back by checking out the previous tag and rebuilding. Migrations applied by the bad release are **not** auto-reverted — review whether they need a manual down migration.

```bash
cd /opt/qalam-backend/Qalam
git checkout v0.x.(y-1)
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --build qalam-api
```

If only the messaging-api needs to be rolled back, name it in the `up` command. If your bad release added a non-reversible migration, contact whoever owns the DB before rolling forward again.

---

## 6. Common one-liners

```bash
# Tail the API logs
docker logs -f --tail 200 qalam-staging-backend-api      # staging
docker logs -f --tail 200 qalam-backend-api              # prod

# Tail just the messaging-api consumer (sees the SMS / email queues drain)
docker logs -f --tail 200 qalam-staging-messaging-api    # staging
docker logs -f --tail 200 qalam-messaging-api            # prod

# RabbitMQ queue depth — useful to confirm OTP SMS / email aren't stuck
docker exec qalam-staging-rabbitmq rabbitmqctl list_queues name messages messages_ready messages_unacknowledged consumers

# Run an ad-hoc EF migration (without touching the long-lived API container)
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod \
  run --rm -e MIGRATE_ON_STARTUP=true qalam-api dotnet ef database update

# Restart only the API (keep RabbitMQ + messaging up)
docker compose -p qalam-staging restart qalam-api

# Free disk space — prune dangling images after several rebuilds
docker image prune -f
```

---

## 7. Common gotchas

- **Always `--env-file`.** Forgetting it makes Compose silently use a *different* set of env values (empty for most) and the API will fail to connect to SQL. Both project names AND env files are required to disambiguate staging from prod on the same host.
- **`MIGRATE_ON_STARTUP=true` is staging-only.** In production keep it `false`; rely on step 3C instead. Don't override in `.env.prod`.
- **`git pull` won't move a detached HEAD.** If you previously did `git checkout v0.1.0`, `git pull` is a no-op. Either `git checkout main && git pull` or `git fetch --tags && git checkout v0.x.y` for tagged releases.
- **Image cache can mask a bad build.** If a build "succeeds" but you don't see your code change live, add `--no-cache` to `docker compose build`: `docker compose -f docker-compose.staging.yml -p qalam-staging build --no-cache qalam-api`.
- **Nginx + TLS are separate.** A failed deploy doesn't break Nginx; a successful deploy doesn't reissue certs. Cert renewal is `sudo certbot renew --dry-run` (auto-runs from systemd timer).
- **Restart the same image quickly.** If you just need to recycle a container (e.g. config env changed in `.env.staging`): `docker compose -p qalam-staging up -d --force-recreate qalam-api`.

---

## 8. Reference scripts in this repo

| Script | Purpose |
|--------|---------|
| [deploy.sh](./deploy.sh) | Convenience wrapper for full local→VPS deploy via rsync (rarely needed if you `git pull` on the VPS) |
| [deploy-now.sh](./deploy-now.sh) | One-shot deploy hook |
| [deploy-webdeploy.sh](./deploy-webdeploy.sh) | Web Deploy variant |
| [scripts/bootstrap-vps-staging.sh](./scripts/bootstrap-vps-staging.sh) | First-time VPS bootstrap (Docker + Nginx + Certbot) |

The day-to-day path is **§2 (staging) → §3 (prod)** above. The scripts exist for special cases.
