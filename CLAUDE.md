# Qalam — CLAUDE.md

Agent-facing reference for the Qalam monorepo (.NET 8 backend). See [AGENTS.md](AGENTS.md) for quick-start commands and the submodule workflow.

Qalam is an educational tutoring marketplace connecting teachers with students and guardians across four domains: school curriculum, Quran, languages, and general skills.

---

## Architecture

```
  Qalam.Api  (controllers, middleware, DI)
    ├── Qalam.Core        (MediatR handlers, validators, AutoMapper, behaviors)
    ├── Qalam.Service     (business logic: pricing, payments, sessions, LRS, storage)
    └── Qalam.Infrastructure (EF Core repos, DbContext, migrations, seeding)
            └── Qalam.Data    (entities, DTOs, enums, Router.cs, settings — leaf, no refs)

  Qalam.MessagingApi      (separate host: RabbitMQ consumers — email, LRS)
  Qalam.Service.Tests     (xUnit)
  apps/admin  (Next.js)  ·  apps/teacher  (Vite)  ·  apps/Qalam  (Flutter)
```

**Dependency rule:** Data ← Infrastructure ← Service ← Core ← Api. Never reference upward.

---

## Project structure

```
Qalam.sln
├── Qalam.Api/               # Controllers, middleware, DI bootstrap
├── Qalam.Core/              # CQRS handlers (MediatR), validators, behaviors
├── Qalam.Service/           # Business logic services
├── Qalam.Infrastructure/    # EF Core repos, DbContext, migrations, seeding
├── Qalam.Data/              # Entities, DTOs, enums, Router.cs, settings
├── Qalam.MessagingApi/      # RabbitMQ consumers (email, LRS)
├── Qalam.Service.Tests/     # xUnit tests
├── apps/admin/              # Next.js 16 admin (submodule)
├── apps/teacher/            # Vite teacher app (submodule)
└── apps/Qalam/              # Flutter student app (submodule)
```

---

## Tech stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 8 / ASP.NET Core 8 |
| CQRS | **MediatR 12.2** (reflection-based, NOT source-generated Mediator) |
| Validation | FluentValidation 11.9 |
| Mapping | AutoMapper |
| Database | SQL Server via EF Core 8 (`UseSqlServer`) |
| Identity | ASP.NET Identity (`IdentityDbContext<User, Role, int>`) |
| Auth | JWT Bearer + refresh tokens |
| Messaging | RabbitMQ (via `RabbitMQ.Client`) |
| Email | MailKit/MimeKit (SMTP) |
| SMS | Twilio |
| Push | Firebase Admin SDK |
| Live video | LiveKit |
| Storage | Alibaba OSS (primary), Wasabi S3 (fallback) |
| Payments | Mock, Moyasar, PayTabs, HyperPay, Stripe (admin-switchable) |
| LRS | NELC xAPI (via RabbitMQ consumer) |
| Logging | Serilog |
| API docs | Swagger + Scalar UI |
| Testing | xUnit |

---

## Request flow

```
HTTP Request
  → Controller (extends AppControllerBase)
    → Mediator.Send(command/query)
      → UserIdentityBehavior (injects current user into request)
      → ValidationBehavior (runs FluentValidation; returns 400 on failure)
      → Handler (extends ResponseHandler)
        → Service layer (business logic)
          → Repository / DbContext
        ← Response<T> (Success / BadRequest / NotFound / …)
    ← NewResult(response) → HTTP status + JSON
```

**MediatR registration** (`ModuleCoreDependencies.cs`):
- `services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()))`
- Pipeline: `UserIdentityBehavior<,>` → `ValidationBehavior<,>`

**Message contracts:** Commands and queries implement `IRequest<Response<T>>`. Handlers return `Task<Response<T>>` and inherit `ResponseHandler` for `Success()`, `BadRequest()`, `NotFound()`, etc.

**Controller pattern:** `return NewResult(await Mediator.Send(new XxxCommand { ... }));`

**Response envelope** (`Response<T>` in `Qalam.Core.Bases`): `StatusCode`, `Succeeded`, `Message`, `Data`, `Meta` (pagination).

---

## Adding a new feature (checklist)

1. **Route constant** → `Qalam.Data/AppMetaData/Router.cs`
2. **DTO** → `Qalam.Data/DTOs/`
3. **Command or Query** → `Qalam.Core/Features/<Area>/Commands|Queries/` implementing `IRequest<Response<T>>`
4. **Handler** → same folder, extending `ResponseHandler`
5. **Validator** → same folder, extending `AbstractValidator<TCommand>`
6. **Controller action** → `Qalam.Api/Controllers/`, returning `NewResult(await Mediator.Send(...))`
7. **Service logic** (if needed) → `Qalam.Service/Implementations/`; register in `ModuleServiceDependencies.cs`
8. **Repository** (if needed) → `Qalam.Infrastructure/Repositories/`; register in `ModuleInfrastructureDependencies.cs`
9. **Migration** (if new column/table) → `dotnet ef migrations add <Name> --project Qalam.Infrastructure --startup-project Qalam.Api`
10. **Tests** → `Qalam.Service.Tests/`

---

## DI registration

| Method | File | Registers |
|--------|------|-----------|
| `AddCoreDependencies()` | `Qalam.Core/ModuleCoreDependencies.cs` | MediatR, AutoMapper, FluentValidation, pipeline behaviors |
| `AddServiceDependencies(config)` | `Qalam.Service/ModuleServiceDependencies.cs` | All business services, payment gateways, LRS, pricing |
| `AddInfrastructureDependencies()` | `Qalam.Infrastructure/ModuleInfrastructureDependencies.cs` | Generic + specific repositories, seeders |
| `AddServiceRegisteration(config)` | `Qalam.Api/Configurations/` | Identity, JWT, Swagger |

---

## Database

- **Provider:** SQL Server via EF Core 8
- **Context:** `ApplicationDBContext` extends `IdentityDbContext<User, Role, int>` with column encryption (`EncryptColumn`)
- **Connection:** `ConnectionStrings:dbcontext` in appsettings / `.env`
- **Repos:** Generic repository (`IGenericRepositoryAsync<>`) + feature-specific repositories
- **Enums:** Stored as `int`. Appending a new value requires **no migration**. Changing values or adding columns does.
- **Seeding:** `DatabaseSeeder.SeedAllAsync` (reference data, idempotent) + `RolesSeeder` + `AdminUserSeeder` (gated by `SEED_DEFAULT_ADMIN=true`)
- **Auto-migration:** Runs on startup (prod gate currently commented out)

```bash
# Add a migration
dotnet ef migrations add <Name> --project Qalam.Infrastructure --startup-project Qalam.Api

# Apply migrations manually
dotnet ef database update --project Qalam.Infrastructure --startup-project Qalam.Api
```

---

## Middleware pipeline (order matters)

SecurityHeaders → ErrorHandler → HTTPS → StaticFiles (`/uploads`) → Routing → CORS → RateLimiting → Authentication → Authorization → BlockedTeacher → AuditLogging → MapControllers

---

## Background services

| Service | Interval | Purpose |
|---------|----------|---------|
| `EnrollmentExpirationService` | Periodic | Expire unpaid enrollments past deadline |
| `OpenSessionRequestLifecycleService` | 5 min | Expire stale OSR invites/offers/requests |
| `SessionLifecycleService` | Periodic | Auto-complete past sessions |
| `PaymentReconciliationBackgroundService` | Periodic | Reconcile pending payment intents |

---

## Environments

| | Local | Staging | Production |
|---|-------|---------|------------|
| Compose | `docker-compose.yml` | `docker-compose.staging.yml` | `docker-compose.prod.yml` |
| Env file | `.env` | `.env.staging` | `.env.prod` |
| API port | 62900 (dotnet) / 8080 (docker) | 8081 | 8080 |
| URL | `localhost` | `api-staging.qalam.net.sa` | `api.qalam.net.sa` |
| Migrations | Auto | Auto | Manual (`MIGRATE_ON_STARTUP`) |

See [DEPLOYMENT.md](DEPLOYMENT.md) for full deploy procedures.

---

## Configuration and secrets

All secrets in `.env` files (never committed). Key groups: `DB_CONNECTION_STRING`, `JWT_SECRET`/`JWT_ISSUER`/`JWT_AUDIENCE`, `RABBITMQ_*`, `EMAIL_*` (SMTP), `OSS_*`/`WASABI_*` (storage, `STORAGE_PROVIDER=alibaba|wasabi`), `PAYMENT_PROVIDER` + gateway keys (`MOYASAR_*`/`PAYTABS_*`/`HYPERPAY_*`/`STRIPE_*`), `LIVEKIT_*`, `FIREBASE_*`, `TWILIO_*`. Full reference: `.env.example`

---

## Localization

- Supported: `en-US`, `ar-EG`
- Gregorian calendar forced for all cultures (prevents Hijri dates in DB)
- Client selects via `?lang=ar|en` query param
- Resource files: `*Resources.ar-EG.resx` / `*Resources.en-US.resx`

---

## Common commands

```bash
dotnet build Qalam.sln
dotnet test Qalam.Service.Tests
dotnet ef migrations add <Name> --project Qalam.Infrastructure --startup-project Qalam.Api

# Local Docker
docker compose up --build

# Staging deploy
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build

# Production deploy
docker compose -f docker-compose.prod.yml -p qalam-prod --env-file .env.prod up -d --build
```

---

## Tests

```bash
dotnet test Qalam.Service.Tests
```

- 487 tests (as of 2026-10-31). 7 pre-existing failures in `EducationFilterServiceRuleTests` and `PaymentIntentReuseTests`.
- Use nullable injection (`IAuditService? audit = null`) for optional constructor params to avoid breaking tests.

---

## Business rules (key points)

- **Education domain codes** — never hardcode entity IDs; use `code` values for lookups.
- **Fixed courses only** — API rejects `isFlexible=true`.
- **Single payer** — group enrollment owner pays full `AmountDue`. Invitees never pay.
- **Pricing snapshots** — frozen at enrollment/offer creation. List/detail use the snapshot, never re-run `EstimateAsync`.
- **Teacher interview** — one unpaid session per teacher account (first completed anywhere).
- **Student free trial** — one lifetime free session credit per student (platform absorbs cost).
- **Cancellation policy** — versioned, immutable per enrollment via `PolicyVersionId` snapshot.

Full rules: [BUSINESS_LOGIC.md](BUSINESS_LOGIC.md)

---

## Rules for Claude

- **Smallest correct diff** — don't refactor surroundings.
- **BUSINESS_LOGIC.md and user story docs are authoritative** for business rules.
- **No secrets in commits** — never commit `.env`, connection strings, API keys.
- **Staging before production** — always validate on staging first.
- **Never hardcode entity IDs** — use `code` values on education entities.
- **Never put business logic in controllers** — controllers only call `Mediator.Send` and return `NewResult`.
- **Use MediatR** — not "Mediator" (source-generated) or any other CQRS library.
- **Response<T> everywhere** — handlers return via `Success()` / `BadRequest()` / etc.; controllers wrap with `NewResult()`.
- **Never edit production config** without explicit request.
- **Nullable injection for tests** — `IAuditService? audit = null` when adding optional deps.

---

## Frontend patterns (`apps/admin`)

- **Framework:** Next.js 16 App Router, React 19, Tailwind 4, Radix UI / base-ui.
- **Data fetching:** `useState` + `useEffect` + `collections/*.ts` helpers. No TanStack Query without discussion.
- **i18n:** Flat dictionary in `lib/locale-context.tsx` (EN + AR). RTL via CSS logical properties.
- **Finance components:** `FinanceDetailLayout`, `FinanceFiltersBar`, `FinanceStatusBadge`, `MoneyAmount`, `FinanceSummaryCards`.
- **Confirm dialogs:** `useConfirm()` from `components/admin/confirm-dialog.tsx`, not `window.confirm`.
- **Submodule commits:** commit inside `apps/admin/` to `qalam-admin` remote, then bump SHA in monorepo.

---

## Feature index

| Feature | Path | Description |
|---------|------|-------------|
| Authentication | `Qalam.Core/Features/Authentication` | Registration, login, OTP, JWT, sessions, profile, device tokens |
| Admin | `Qalam.Core/Features/Admin` | Teacher mgmt, finance, pricing, sessions, complaints, LRS, legal docs, policies, wallets, nationalities |
| Student | `Qalam.Core/Features/Student` | Course catalog, enrollments, enrollment requests, OSR, payments, wallet, sessions, identity, children |
| Teacher | `Qalam.Core/Features/Teacher` | Course mgmt, enrollment requests/enrollments, OSR offers, sessions, finance, profile, availability, subjects, content |
| Education | `Qalam.Core/Features/Education` | Domains, levels, grades, terms, universities, colleges, departments, programs, writable filters |
| Content | `Qalam.Core/Features/Content` | Content units and lessons CRUD |
| Curriculum | `Qalam.Core/Features/Curriculum` | Curriculum CRUD |
| Subjects | `Qalam.Core/Features/Subjects` | Subject CRUD |
| Quran | `Qalam.Core/Features/Quran` | Quran levels, content types, parts, surahs |
| Teaching | `Qalam.Core/Features/Teaching` | Time slots, days of week, session types, teaching modes |
| Legal | `Qalam.Core/Features/Legal` | Legal documents, consents |
| Messaging | `Qalam.Core/Features/Messaging` | Email, SMS, push notifications |
| Notifications | `Qalam.Core/Features/Notifications` | In-app notification inbox |
| Contact | `Qalam.Core/Features/Contact` | Contact form submissions |
| Common/Pricing | `Qalam.Core/Features/Common/Pricing` | Shared pricing estimate queries |

---

## Authoritative docs

| Topic | File |
|-------|------|
| Business logic | [BUSINESS_LOGIC.md](BUSINESS_LOGIC.md) |
| Architecture | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| LRS / xAPI | [docs/LRS.md](docs/LRS.md) |
| User stories | [docs/USER-STORIES-Scenarios-1-and-2.md](docs/USER-STORIES-Scenarios-1-and-2.md) |
| Deployment | [DEPLOYMENT.md](DEPLOYMENT.md) |
| Agent quick-start | [AGENTS.md](AGENTS.md) |
| Enrollment flow | [ENROLLMENT_FLOW.md](ENROLLMENT_FLOW.md) |
| Operations | [docs/OPERATIONS_RUNBOOK.md](docs/OPERATIONS_RUNBOOK.md) |
