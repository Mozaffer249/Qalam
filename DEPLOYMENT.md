# Deployment

Follow these **in order** on the VPS. All guides live in [`docs/deployment/`](./docs/deployment/README.md).

| Step | Guide |
|------|--------|
| 1 | [`01-sql-server-install.md`](./docs/deployment/01-sql-server-install.md) — SQL Server 2022, loopback only, four DBs |
| — | [`sql-bootstrap-qalam.sql`](./docs/deployment/sql-bootstrap-qalam.sql) — run during step 1 §6 |
| 2 | [`02-after-sql-bootstrap.md`](./docs/deployment/02-after-sql-bootstrap.md) — verify SQL → staging Docker → HTTPS |
| 3 | [`03-staging-setup.md`](./docs/deployment/03-staging-setup.md) — `api-staging.qalam.net.sa` |
| 4 | [`04-production-setup.md`](./docs/deployment/04-production-setup.md) — `api.qalam.net.sa` |
| 5 | [`05-nginx-subdomains.md`](./docs/deployment/05-nginx-subdomains.md) — frontend SPA vhosts + Certbot |

**Day-2 ops:** [`docs/OPERATIONS_RUNBOOK.md`](./docs/OPERATIONS_RUNBOOK.md)

**Local Docker dev:** [`DOCKER_README.md`](./DOCKER_README.md)
