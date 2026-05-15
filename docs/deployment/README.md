# VPS deployment guides (run in order)

All production/staging setup docs live in this folder. Follow the numbered files on the VPS.

**One git clone** on the server (e.g. `/opt/qalam-backend/Qalam`): production and staging are separate Docker Compose projects in the same directory (`.env` / `.env.prod` vs `.env.staging`).

| Order | File | What |
|-------|------|------|
| 1 | [`01-sql-server-install.md`](./01-sql-server-install.md) | Install SQL Server 2022 on Ubuntu, bind `127.0.0.1:1433`, backups |
| — | [`sql-bootstrap-qalam.sql`](./sql-bootstrap-qalam.sql) | Run during step 1 §6 (four DBs + two logins, password `Qalam@2026`) |
| 2 | [`02-after-sql-bootstrap.md`](./02-after-sql-bootstrap.md) | Verify SQL → staging Docker → Nginx → HTTPS (quick path) |
| 3 | [`03-staging-setup.md`](./03-staging-setup.md) | `api-staging.qalam.net.sa` (auto-migrate, demo admin) |
| 4 | [`04-production-setup.md`](./04-production-setup.md) | `api.qalam.net.sa` (manual migration gate, prod secrets) |
| 5 | [`05-nginx-subdomains.md`](./05-nginx-subdomains.md) | Frontend SPA vhosts + consolidated Certbot |

**Day-2 ops:** [`../OPERATIONS_RUNBOOK.md`](../OPERATIONS_RUNBOOK.md)

**Repo entry point:** [`../../DEPLOYMENT.md`](../../DEPLOYMENT.md)

**Local Docker dev:** [`../../DOCKER_README.md`](../../DOCKER_README.md)
