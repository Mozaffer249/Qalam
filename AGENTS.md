# Qalam Platform — Agent Instructions

Education platform monorepo: .NET 8 backend + Admin (Next.js) + Teacher (Vite) + Student (Flutter).

## Quick reference

| Topic | Location |
|-------|----------|
| Architecture | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| Contributing | [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) |
| Business logic | [BUSINESS_LOGIC.md](BUSINESS_LOGIC.md) |
| User stories S1/S2 | [docs/USER-STORIES-Scenarios-1-and-2.md](docs/USER-STORIES-Scenarios-1-and-2.md) |
| Deployment | [DEPLOYMENT.md](DEPLOYMENT.md) |
| Cursor rules | [.cursor/rules/](.cursor/rules/) |
| Rules generator | [docs/CURSOR-RULES-PROMPT.md](docs/CURSOR-RULES-PROMPT.md) |
| Student app agent | [apps/Qalam/AGENTS.md](apps/Qalam/AGENTS.md) |

## Local dev commands

```powershell
# From repo root — load .env automatically
.\scripts\dev\run-rabbitmq.ps1
.\scripts\dev\run-messaging-api.ps1    # :62901
.\scripts\dev\run-api.ps1              # :62900
.\scripts\dev\run-admin.ps1            # :3005
.\scripts\dev\run-teacher.ps1          # :3000
.\scripts\dev\run-student-flutter.ps1
```

```bash
docker compose up --build qalam-api messaging-api rabbitmq
dotnet build Qalam.sln
dotnet test Qalam.Service.Tests
```

## Key backend files

| File | Purpose |
|------|---------|
| `Qalam.Api/Program.cs` | API bootstrap |
| `Qalam.Data/AppMetaData/Router.cs` | Route constants (`/Api/V1/`) |
| `Qalam.Infrastructure/context/ApplicationDBContext.cs` | EF Core |
| `Qalam.Core/Features/` | CQRS handlers |

## Key frontend env vars

| App | Env var | Example |
|-----|---------|---------|
| Admin | `NEXT_PUBLIC_API_URL` | `http://localhost:62900` |
| Teacher | `VITE_API_URL` | `http://localhost:62900` |
| Student | Flavor config | `lib/main_development.dart` |

## When implementing

### Backend endpoint
1. `Router.cs` constant → DTO → Command/Handler/Validator → Controller
2. Return `Response<T>`; validate with FluentValidation
3. Use education domain `code` values — never hardcode ids

### Admin / Teacher (submodules)
- Commit inside `apps/admin/` or `apps/teacher/` to their own repos
- Bump submodule SHA in monorepo after merge

### Student Flutter
- Follow `apps/Qalam/.cursor/rules/` and `apps/Qalam/docs/CONTRIBUTING.md`

## Principles

- Smallest correct diff
- `BUSINESS_LOGIC.md` and user story docs are authoritative
- No secrets in commits
- Staging before production for deploy changes
