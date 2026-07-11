# Qalam Platform â€” Monorepo

This repository is the entry point for the Qalam platform. The backend (.NET 8) lives at the root; the frontend apps live under `apps/` as **git submodules** so each one keeps its own GitHub repo, its own commits, and its own release cadence.

## Layout

| Path | Tech | Source | Dev port |
| --- | --- | --- | --- |
| `Qalam.Api/`, `Qalam.Service/`, `Qalam.Core/`, `Qalam.Data/`, `Qalam.Infrastructure/`, `Qalam.MessagingApi/`, `Qalam.Service.Tests/` | .NET 8 / EF Core | This repo | 8080 (compose) |
| `apps/admin/` | Next.js 16 + React 19 + Tailwind 4 | submodule â†’ `github.com/Mozaffer249/qalam-admin` | 3005 |
| `apps/teacher/` | Vite 7 + TanStack Start/Router + React 19 + Tailwind 4 | submodule → `github.com/Mozaffer249/qalam-teacher` | 3000 |
| `apps/Qalam/` | Flutter (student app) | submodule → `github.com/ahmedhantop/qalam` | — |

## Cloning

First clone â€” pull everything including submodules:

```bash
git clone --recurse-submodules https://github.com/Mozaffer249/Qalam.git
```

Already cloned without submodules?

```bash
git submodule update --init --recursive
```

If `apps/admin`, `apps/teacher`, or `apps/Qalam` ever looks empty after a `git pull`, run the same command above.

## Running locally

### Backend (Docker)

```bash
cp .env.example .env   # fill in the secrets
docker compose up --build qalam-api messaging-api rabbitmq
```

### Frontend apps (host, for dev with hot reload)

Point frontends at the API URL you use:

| API mode | URL | Admin `NEXT_PUBLIC_API_URL` / Teacher `VITE_API_URL` |
| --- | --- | --- |
| Docker `qalam-api` | http://localhost:8080 | `http://localhost:8080` |
| Native `dotnet run` / VS F5 | http://localhost:62900 | `http://localhost:62900` |

```bash
# admin (Next.js, port 3005)
cd apps/admin
npm install
npm run dev

# teacher (Vite + TanStack Start, port 3000)
cd apps/teacher
npm install
npm run dev
```

### Run each project individually (Windows PowerShell)

From repo root, in **separate terminals** (order matters for messaging):

```powershell
# 1) RabbitMQ only (Docker)
.\scripts\dev\run-rabbitmq.ps1

# 2) Messaging API â†’ http://localhost:62901
.\scripts\dev\run-messaging-api.ps1

# 3) Main API â†’ http://localhost:62900
.\scripts\dev\run-api.ps1

# 4) Admin â†’ http://localhost:3005
.\scripts\dev\run-admin.ps1

# 5) Teacher â†’ http://localhost:3000
.\scripts\dev\run-teacher.ps1
```

Scripts load secrets from root `.env`. VS can also F5 **Qalam.Api** (port 62900) after setting user secrets or env vars from `.env`.

### Full stack via Docker

```bash
docker compose up --build
```

This builds the backend image plus the two frontend images using the Dockerfiles inside `apps/admin/` and `apps/teacher/`. Both directories must be populated (see [Cloning](#cloning)).

## Development workflow

Each frontend is still an independent repo with independent commits and pushes:

- **Working on admin** â€” open `apps/admin/` (or your separate clone of `qalam-admin`) in your editor. `git pull / commit / push` inside that directory talks to `github.com/Mozaffer249/qalam-admin`, not to the monorepo.
- **Working on teacher** â€” same, but for `github.com/Mozaffer249/qalam-teacher`.
- **Working on backend** â€” open this repo at its root. `git pull / commit / push` here talks to `github.com/Mozaffer249/Qalam`.

### Bumping a frontend version in the monorepo

When you want the monorepo (and its docker-compose builds) to pick up a new frontend revision:

```bash
# Pull the latest in the submodule
cd apps/admin
git pull origin main
cd ../..

# Record the new SHA in the monorepo
git add apps/admin
git commit -m "chore: bump admin submodule"
git push
```

Or, to bump all submodules to the tracked branch HEAD in one step:

```bash
git submodule update --remote --merge
git add apps/admin apps/teacher
git commit -m "chore: bump frontend submodules"
git push
```

## CI / deploy notes

Any CI job that builds the full stack must check out submodules. With GitHub Actions:

```yaml
- uses: actions/checkout@v4
  with:
    submodules: recursive
```

Existing CI/CD pipelines that build only one frontend (Vercel, GitHub Actions in the standalone repos) keep working unchanged â€” they build directly from `Mozaffer249/qalam-admin` or `Mozaffer249/qalam-teacher`.

## Rollback

A `pre-monorepo` tag was created on the backend repo just before submodules were introduced. To revert:

```bash
git reset --hard pre-monorepo
git submodule deinit -f apps/admin apps/teacher
rm -rf .git/modules/apps
rm -rf apps
```
