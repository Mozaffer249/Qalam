# Deployment

The previous version of this file documented an early single-stack VPS deploy on Hostinger. It is **superseded** by the staging + production setup in [`docs/`](./docs/).

Follow these in order:

1. **[`docs/SQL_SERVER_INSTALL.md`](./docs/SQL_SERVER_INSTALL.md)** — install SQL Server 2022 natively on Ubuntu 22.04, bound to 127.0.0.1 only, with the four databases + two SQL logins used by both environments.
2. **[`docs/STAGING_SETUP.md`](./docs/STAGING_SETUP.md)** — bring up `api-staging.qalam.net.sa` (auto-migrate on, demo admin seeded).
3. **[`docs/PRODUCTION_SETUP.md`](./docs/PRODUCTION_SETUP.md)** — bring up `api.qalam.net.sa` (manual migration, default-admin gated).
4. **[`docs/OPERATIONS_RUNBOOK.md`](./docs/OPERATIONS_RUNBOOK.md)** — day-2 ops: migrations, backups, restore, secret rotation, log lookups, common failure modes.

Reference material (read once, then forget):

- [`DOCKER_README.md`](./DOCKER_README.md) — local-dev Docker Compose flow.
- [`NGINX_SUBDOMAIN_DEPLOYMENT.md`](./NGINX_SUBDOMAIN_DEPLOYMENT.md) — Nginx vhost patterns. The staging/prod guides above include the specific vhost blocks you actually need.

If anything in `docs/` ever drifts from reality, fix it there — this file should remain a one-screen pointer.
