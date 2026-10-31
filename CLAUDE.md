# Qalam — CLAUDE.md

Agent-facing reference for the Qalam monorepo. See [AGENTS.md](AGENTS.md) for quick-start commands and the submodule workflow.

---

## Project structure

```
Qalam.sln
├── Qalam.Api/               # ASP.NET Core 8 — entry point, controllers, DI bootstrap
├── Qalam.Core/              # CQRS handlers (MediatR), validators
├── Qalam.Service/           # Business logic services, implementations
├── Qalam.Infrastructure/    # EF Core repositories, DB context, migrations
├── Qalam.Data/              # Entities, DTOs, enums, route constants, settings POCOs
├── Qalam.MessagingApi/      # Separate .NET host — RabbitMQ consumers (email, LRS)
├── Qalam.Service.Tests/     # xUnit test project
├── apps/admin/              # Next.js 16 admin app (git submodule → qalam-admin)
├── apps/teacher/            # Vite teacher app (git submodule)
└── apps/Qalam/              # Flutter student app (git submodule)
```

---

## Backend patterns

### Adding a new endpoint

1. Add route constant to `Qalam.Data/AppMetaData/Router.cs` (under the relevant group).
2. Add/extend DTO in `Qalam.Data/DTOs/`.
3. Add Command or Query + Handler + Validator under `Qalam.Core/Features/`.
4. Add controller action returning `Response<T>` via `NewResult(await Mediator.Send(...))`.
5. Implement service logic in `Qalam.Service/Implementations/`; register in `ModuleServiceDependencies.cs`.
6. Add repository method if needed; register in `Qalam.Infrastructure/ModuleInfrastructureDependencies.cs`.

### Enums

EF Core stores all enums as `int`. **Appending a new enum value requires NO migration.** Changing existing values or adding a new entity column does require a migration (`dotnet ef migrations add <Name>` from the startup project).

### Response wrapper

All API responses use `Response<T>` (in `Qalam.Core.Bases`). Return `Success(entity: data)` or `BadRequest(message)` etc. from handlers. The controller calls `NewResult(response)`.

### Education domain codes

Never hardcode entity IDs in logic. Use the `code` value on education entities (courses, schedules, etc.) for domain lookups.

---

## Frontend patterns (`apps/admin`)

- **Framework:** Next.js 16 App Router, React 19, Tailwind 4, Radix UI / base-ui.
- **Data fetching:** `useState` + `useEffect` + `collections/*.ts` fetch helpers. Do **not** introduce TanStack Query without discussion.
- **i18n:** Single flat dictionary in `lib/locale-context.tsx` (EN + AR). All new keys go there. RTL via CSS logical properties + `direction` from `useLocale()`.
- **Finance kit components:** `FinanceDetailLayout`, `FinanceFiltersBar`, `FinanceStatusBadge`, `RelatedEntityLinks`, `MoneyAmount`, `FinanceSummaryCards` — reuse these for any finance-adjacent page.
- **Confirm dialogs:** use `useConfirm()` from `components/admin/confirm-dialog.tsx`, not `window.confirm`.
- **Status labels/colours:** add to `lib/finance-labels.ts`; use `FinanceStatusBadge` for rendering.
- **Submodule commits:** commit inside `apps/admin/` to the `qalam-admin` remote, then bump the submodule SHA in the monorepo.

---

## Student Identity (NELC compliance)

Students can set a national identity document (National ID for Saudi, Iqama/Passport for non-Saudi) used as the xAPI actor identifier for NELC compliance.

- **Entity fields** on `Student`: `IdentityType` (enum), `IdentityNumber` (max 50), `NationalityId` (FK → Nationality), `IssuingCountryCode` (max 3). Added in migration `20261031145637_AddStudentIdentityFields`.
- **Validation rules** stored in `SystemSetting` key `StudentIdentityRules` (JSON). Rules map nationality codes to allowed identity types. Default: Saudi → NationalId only; others → Iqama or Passport (issuing country required).
- **Service:** `IStudentIdentityService` / `StudentIdentityService` — loads rules, resolves allowed types by nationality.
- **Business rules:** `StudentIdentityBusinessRules` — type-allowed, issuing-country, uniqueness checks.
- **API endpoints** (`StudentIdentityController`): `POST/GET Student/Identity`, `GET Student/Identity/AllowedTypes`, `PUT Student/Children/{id}/Identity`.
- **xAPI integration:** `XApiStatementBuilder.BuildActor` prefers `identityNumber` as `account.name` when set; `LrsDispatchService.ResolveStudentUserAsync` returns `(User, IdentityNumber)` tuple.
- **Flutter:** `ChildNationalIdentitySection` widget (nationality → identity type → number → issuing country). Wired into add child and edit child screens.
- **Admin panel:** Student list shows `identityNumber` column. Student detail page has an "Identity" card (type, number, nationality, issuing country). Enrollment detail page shows `teacherActor` / `studentActor` profile cards with contact info and identity data via `AdminEnrollmentActorDto`.

---

## LRS (Learning Record Store / xAPI)

Full reference: [docs/LRS.md](docs/LRS.md).

**Summary for agents:**

- Qalam reports learning events to an xAPI-compliant LRS (NELC) via RabbitMQ.
- Controlled by `LrsSettings.Enabled` in `appsettings.json` / environment.
- **Dispatch** happens in `LrsDispatchService` (called from `SessionLifecycleHelper`, `EnrollmentCompletionService`, `PaymentConfirmationService`, etc.).
- **Delivery** is handled by `Qalam.MessagingApi`'s `LrsStatementConsumer`: reads `lrs-statements` queue, POSTs to LRS endpoint (Basic auth + `X-Experience-API-Version: 1.0.3`), retries up to `MaxRetries` on transient errors, sends permanent failures to `lrs-statements.dlq`.
- **Tracked verbs:** `registered` (enrollment), `initialized` (session launched), `attended`, `completed` (session + course), `progressed`.
- **Admin monitoring:** `/lrs` page in the admin app; API under `/Api/V1/Admin/Lrs/` (dashboard, list, detail, retry).
- **DB entity:** `LrsStatementLog` — added in migration `20261029211411_AddLrsStatementLog`.
- **Config keys** (all under `LrsSettings` section): `Enabled`, `Endpoint`, `Key`, `Secret`, `MaxRetries`, `PlatformIri`, `XApiVersion`, `PlatformKey`, `PlatformNameAr`, `PlatformNameEn`. `Key`/`Secret` are credentials — never commit.

When modifying LRS dispatch (adding a new verb or trigger):
1. Add a method to `ILrsDispatchService` + `LrsDispatchService`.
2. Add the builder method to `IXApiStatementBuilder` + `XApiStatementBuilder`.
3. Call the dispatch service from the appropriate service/handler.
4. Update `docs/LRS.md` tracked-events table.

---

## Key env vars

| App | Var | Example |
|-----|-----|---------|
| Admin (Next.js) | `NEXT_PUBLIC_API_URL` | `http://localhost:62900` |
| Teacher (Vite) | `VITE_API_URL` | `http://localhost:62900` |
| Backend | `LrsSettings__Enabled` | `true` |
| Backend | `LrsSettings__Endpoint` | `https://lrs.nelc.gov.sa/xapi/statements` |
| Backend | `LrsSettings__Key` / `LrsSettings__Secret` | (never commit) |

---

## Tests

```bash
dotnet test Qalam.Service.Tests
```

- 487 tests total as of 2026-10-31 (7 pre-existing failures in `EducationFilterServiceRuleTests` and `PaymentIntentReuseTests` — unrelated to LRS or finance redesign changes).
- New tests live alongside the feature under `Qalam.Service.Tests/` (e.g. `RefundApprovalWorkflowTests`).
- When adding optional constructor parameters to services to avoid breaking tests, use nullable injection (`IAuditService? audit = null`).

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
