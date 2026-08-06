# Contributing to Qalam

## Prerequisites

| Component | Requirement |
|-----------|-------------|
| Backend | .NET 8 SDK, SQL Server access |
| Admin | Node.js 20+, npm |
| Teacher | Node.js 20+, npm |
| Student | Flutter 3.7+ |
| Local stack | Docker (RabbitMQ, optional full compose) |

## Clone

```bash
git clone --recurse-submodules https://github.com/Mozaffer249/Qalam.git
cd Qalam
cp .env.example .env   # fill secrets
```

If submodules are empty: `git submodule update --init --recursive`

## Running locally

### Backend only (Docker)

```bash
docker compose up --build qalam-api messaging-api rabbitmq
```

### Full native stack (Windows)

```powershell
.\scripts\dev\run-rabbitmq.ps1
.\scripts\dev\run-messaging-api.ps1
.\scripts\dev\run-api.ps1
.\scripts\dev\run-admin.ps1
.\scripts\dev\run-teacher.ps1
.\scripts\dev\run-student-flutter.ps1
```

### Frontend env URLs

| API mode | URL |
|----------|-----|
| Docker | `http://localhost:8080` |
| Native dotnet | `http://localhost:62900` |

Set `NEXT_PUBLIC_API_URL` (admin) and `VITE_API_URL` (teacher) accordingly.

## Where to make changes

| Change type | Work in | Commit to |
|-------------|---------|-----------|
| API endpoint / domain logic | Root repo (`Qalam.*`) | `Mozaffer249/Qalam` |
| Admin UI | `apps/admin/` | `Mozaffer249/qalam-admin` |
| Teacher UI | `apps/teacher/` | `Mozaffer249/qalam-teacher` |
| Student app | `apps/Qalam/` | Student app repo |

After frontend changes, bump the submodule pointer in the monorepo.

## Backend checklist (new endpoint)

- [ ] Route constant in `Qalam.Data/AppMetaData/Router.cs`
- [ ] DTO in `Qalam.Data/DTOs/`
- [ ] Command/Query + Handler + Validator in `Qalam.Core/Features/`
- [ ] Controller action in `Qalam.Api/Controllers/`
- [ ] Entity/migration if schema changes (`Qalam.Infrastructure`)
- [ ] Postman collection update if applicable
- [ ] Feature doc in `docs/` if new user-facing flow

## Frontend checklists

### Admin

- [ ] API call uses `NEXT_PUBLIC_API_URL` + `/Api/V1/...`
- [ ] TanStack collection or query pattern matches existing `collections/`
- [ ] Check `Response<T>` `succeeded` before using `data`

### Teacher

- [ ] API call uses `VITE_API_URL`
- [ ] Route-local `-queries/` / `-api/` colocated with route
- [ ] Zod validation on forms

### Student (Flutter)

See `apps/Qalam/docs/CONTRIBUTING.md`:

- [ ] Feature under `lib/features/<name>/` clean architecture
- [ ] Route in `routes_sercives.dart`
- [ ] Strings in `ar.json` + `en.json` + `app_strings.dart`
- [ ] Endpoint in `api_endpoints.dart`

## Analysis & tests

```bash
# Backend
dotnet build Qalam.sln
dotnet test Qalam.Service.Tests

# Student
cd apps/Qalam && flutter analyze
```

## Documentation

- Add feature-specific guides to `docs/` — link from `docs/ARCHITECTURE.md`
- Do not duplicate content already in `BUSINESS_LOGIC.md` or user story docs
- Update `AGENTS.md` if agent workflows change

## AI-assisted development

Cursor rules live in `.cursor/rules/` (monorepo) and `apps/Qalam/.cursor/rules/` (student app). Open the repo at `C:\dev\Qalam` root for full context.

Reusable rules generator: [CURSOR-RULES-PROMPT.md](./CURSOR-RULES-PROMPT.md)

## Secrets

Never commit `.env`, tokens, or credentials. Use `.env.example` as the template.
