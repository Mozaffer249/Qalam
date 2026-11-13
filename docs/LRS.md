# LRS — Learning Record Store Integration

Qalam reports learning activity to an external **xAPI-compliant Learning Record Store** (NELC / any ADL-compatible LRS). This document describes what is tracked, how the pipeline works, and how to operate it.

---

## Overview

Every significant student learning event — enrolling in a course, attending a session, completing a session, progressing through a course, finishing a course — is serialised as an **xAPI statement** and delivered to the configured LRS endpoint. The statements follow the [xAPI 1.0.3](https://adlnet.gov/projects/xapi/) specification with NELC-specific context extensions.

The feature is **toggle-controlled** via `LrsSettings.Enabled`. When disabled the dispatch service returns immediately and no RabbitMQ messages are queued.

---

## Architecture

```
Qalam.Api / Service layer
        │
        ▼
LrsDispatchService        ← builds statement, publishes to RabbitMQ
        │
        ▼ (queue: lrs-statements)
RabbitMQ
        │
        ▼
LrsStatementConsumer      ← reads queue, POSTs to LRS endpoint
(Qalam.MessagingApi)      ← on HTTP 4xx → DLQ; on network error → retry up to MaxRetries
        │
        ├── success  → BasicAck
        ├── 4xx      → DLQ (lrs-statements.dlq) + BasicAck
        └── retry    → republish with x-retry-count header
```

The consumer uses a separate `NelcLrs` named `HttpClient` (registered in `Qalam.MessagingApi`). The `LrsStatementLog` database table is a **read-side log** maintained by the admin API, NOT by the consumer — the consumer writes nothing to the database directly.

> **Note:** As of 2026-10-31 `LrsStatementLog` is populated by the admin query layer from RabbitMQ message metadata. The consumer itself only POSTs to the LRS and retries; it does not write to SQL Server.

---

## Tracked events (verbs)

Qalam emits a **course-level** NELC journey (NELC's content verbs — watched/attempted/lesson/module — are deferred until a video/content feature exists). Every statement's actor is `{ name: nationalId, mbox: mailto:email }` (single IFI), every object-is-course statement carries **no** `contextActivities` (NELC rule CRS002), durations are full ISO 8601 `PT{H}H{M}M{S}S` (CRS004), and ids are deterministic so re-dispatch is idempotent (SEQ003).

| Trigger | Verb IRI | Object | Method |
|---------|----------|--------|--------|
| Enrollment activated (payment confirmed) | `http://adlnet.gov/expapi/verbs/registered` | course | `DispatchEnrollmentActivatedAsync` |
| Enrollment activated (payment confirmed) | `http://adlnet.gov/expapi/verbs/initialized` | course | `DispatchEnrollmentActivatedAsync` |
| Progress updated after each session | `http://adlnet.gov/expapi/verbs/progressed` | course | `DispatchProgressUpdatedAsync` |
| Course fully completed | `http://adlnet.gov/expapi/verbs/completed` | course | `DispatchCourseCompletedAsync` |
| Course fully completed | `http://id.tincanapi.com/verb/earned` | certificate | `DispatchCourseCompletedAsync` |
| Student submits a review | `http://id.tincanapi.com/verb/rated` | course | `DispatchStudentRatedAsync` |

### Who calls the dispatch service

| Service | Events dispatched |
|---------|------------------|
| `PaymentConfirmationService` | enrollment-activated → registered + initialized |
| `EnrollmentCompletionService` | course-completed → completed + earned |
| `SetSessionAttendanceCommandHandler` / `AdminSessionActionService` | progress-updated → progressed |
| `SessionReviewService` | rated (on student review submission) |

`ReplayFullJourneyAsync` (admin manual dispatch) replays the whole sequence in order: registered → initialized → progressed → completed → earned → rated.

---

## xAPI statement structure

Statements are composed from small reusable builders in `Qalam.Service/Implementations/LRS/Building/` (`XApiActorFactory`, `NelcActivityFactory`, `NelcContextFactory`, `NelcExtensionsFactory`, `XApiDuration`, `XApiStatementId`); `XApiStatementBuilder` is a thin orchestrator.

**Actor** — `{ name: nationalId, mbox: mailto:{email} }` when the student has a national identity set. Falls back to `account` (homePage + nationalId) when there is no email. Exactly one inverse-functional identifier (never mbox + account together).

**Object IRI**:
- Course: `{PlatformIri}/course/{courseId}` (type cmi5 course)
- Certificate: `{PlatformIri}/course/{courseId}/certificate/{enrollmentId}` (type certificate)

**Context** — course-object statements carry only `platform` + `language` (+ instructor/extensions on registered/initialized), **never `contextActivities`** (CRS002). The certificate (`earned`) statement carries `contextActivities.parent = [course]`.

**Registered extensions**: `duration` (ISO 8601 course total), `lms_url`, `program_url` = `{PlatformIri}/course/{courseId}`, `platform` = `{ name: { ar-SA, en-US } }`.

**Result**:
- `progressed`: `score.scaled = completedSessions/totalSessions`, `completion`
- `rated`: `score { scaled = rating/5, raw = rating, min 0, max 5 }`, `response = feedback`
- `completed` (course) / `registered` / `initialized` / `earned`: no result

---

## NELC validator rules (enforced by `Building/NelcStatementValidator`, run before queuing)

- **Actor**: exactly one inverse-functional identifier — `mbox: mailto:{email}` with `name = national ID` (account fallback only when no email). Never mbox + account together.
- **Course description** (`definition.description`): only on the `registered` statement; omitted everywhere else (CTX001 / CRS008).
- **contextActivities**: course-object statements carry none (CRS002); only `earned` (object = certificate) has `parent: [course]`.
- **Durations**: full ISO-8601 `PT{H}H{M}M{S}S` (CRS004).
- **Instructor**: `context.instructor = { name: "<teacher full name>", mbox }` (real name, not email).
- **`rated`**: must carry a non-empty `result.response` (ASM004). A rating with no comment is simply not reported.
- **Statement id**: deterministic per (platform, learner, verb, object) so re-dispatch is idempotent (SEQ003).

## Certificate endpoint (`earned` reachability — CRT002)

NELC fetches the `earned` statement's `jws-certificate-location`, so it must return 200. Served by
`[AllowAnonymous] GET /Api/V1/Lrs/Certificate/{enrollmentId}` (minimal HTML certificate). The location is
`{CertificateBaseUrl}/Api/V1/Lrs/Certificate/{enrollmentId}`; set **`CertificateBaseUrl`** (LRS settings,
admin-managed) to the reachable public API origin (e.g. `https://api-staging.qalam.net.sa`) — if empty it
falls back to `PlatformIri` and will 404.

## Required learner data (enforced at the source)

- **National identity** — required at registration (Flutter self-register + add-child block submit without it;
  national ID is the xAPI `actor.name`).
- **Review feedback** — required, minimum 15 characters (Flutter review form gates the Send button;
  `SubmitStudentSessionReviewCommandValidator` + `SessionReviewService` enforce server-side), so every
  `rated` statement has a text response.

**Deferred** (no backing data until a video/quiz/content feature exists): `watched` (video), `attempted`
(quiz), `completed`→lesson/module.

---

## Configuration (`appsettings.json` / environment)

Section name: **`LrsSettings`**

| Key | Default | Notes |
|-----|---------|-------|
| `Enabled` | `false` | Set `true` in staging/production |
| `Endpoint` | — | Full URL of the xAPI statements endpoint |
| `Key` | — | Basic-auth username (NELC-assigned) |
| `Secret` | — | Basic-auth password — **never commit** |
| `MaxRetries` | `3` | Consumer retries before DLQ |
| `PlatformIri` | `https://qalam.net.sa` | Base IRI for object/actor homePage |
| `XApiVersion` | `1.0.3` | Sent in `X-Experience-API-Version` header |
| `PlatformKey` | — | NELC-assigned platform identifier (e.g. `QLAM-001`) |
| `PlatformNameAr` | `قلم` | Arabic name in NELC context extensions |
| `PlatformNameEn` | `Qalam` | English name in NELC context extensions |
| `CertificateBaseUrl` | — | Reachable public API origin serving the certificate endpoint (falls back to `PlatformIri`). Required for `earned`/CRT002. |

Docker Compose passes these as environment variables prefixed `LrsSettings__` (double underscore = nested key in .NET).

---

## RabbitMQ queues

| Queue | Purpose |
|-------|---------|
| `lrs-statements` | Main delivery queue (durable, persistent messages) |
| `lrs-statements.dlq` | Dead-letter queue — statements that failed permanently (HTTP 4xx) or exhausted retries |

The consumer uses `prefetchCount: 1` (at-most-one-in-flight per worker). Retries are implemented by republishing with an `x-retry-count` header; no native RabbitMQ DLX is required.

---

## Database entity: `LrsStatementLog`

Table registered in `ApplicationDBContext` via the `20261029211411_AddLrsStatementLog` migration.

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int (PK, identity) | |
| `StatementId` | Guid | xAPI statement UUID |
| `Verb` | nvarchar(64) | Short verb label (e.g. `completed`) |
| `ActorIdentifier` | nvarchar(256) | Email or `user-{id}` |
| `ObjectIri` | nvarchar(512) | |
| `Status` | int | `0`=Queued `1`=Sent `2`=Failed `3`=DeadLettered |
| `HttpStatusCode` | int? | LRS HTTP response code |
| `ErrorMessage` | nvarchar(2000) | Last error |
| `RetryCount` | int | |
| `CreatedAt` | datetime | UTC |
| `SentAt` | datetime? | UTC — set on success |
| `SourceEntityType` | nvarchar(64) | `CourseSchedule` or `Enrollment` |
| `SourceEntityId` | int | FK value into that entity |
| `StatementJson` | nvarchar(max)? | Full serialised xAPI statement |

---

## Admin interface

Route group: `/Api/V1/Admin/Lrs` — requires `SuperAdmin` or `Admin` role.

| Method | Route | Handler |
|--------|-------|---------|
| `GET` | `/Api/V1/Admin/Lrs/Dashboard` | `GetLrsDashboardQuery` |
| `GET` | `/Api/V1/Admin/Lrs/Statements` | `ListLrsStatementsQuery` (paginated, filterable) |
| `GET` | `/Api/V1/Admin/Lrs/Statements/{id}` | `GetLrsStatementByIdQuery` |
| `POST` | `/Api/V1/Admin/Lrs/Statements/{id}/Retry` | `RetryLrsStatementCommand` |

### Dashboard response (`AdminLrsDashboardDto`)

```json
{
  "totalStatements": 1420,
  "sentCount": 1380,
  "failedCount": 15,
  "queuedCount": 5,
  "deadLetteredCount": 20,
  "lastSentAt": "2026-10-30T14:22:00Z",
  "statementsByVerb": {
    "registered": 200,
    "completed": 600,
    "progressed": 400,
    "attended": 220
  },
  "failureRate": 0.025
}
```

### List filter parameters (`AdminLrsStatementFilter`)

| Param | Type | Notes |
|-------|------|-------|
| `status` | int? | 0–3 |
| `verb` | string? | e.g. `completed` |
| `studentId` | int? | |
| `enrollmentId` | int? | |
| `courseScheduleId` | int? | |
| `dateFrom` / `dateTo` | string? | ISO 8601 |
| `pageNumber` / `pageSize` | int | default 1 / 20 |

### Retry

`POST /Api/V1/Admin/Lrs/Statements/{id}/Retry` — resets a `Failed` or `DeadLettered` statement back to `Queued` and republishes it to the RabbitMQ queue. The consumer will redeliver it to the LRS endpoint.

---

## Frontend — Admin LRS page

**Route:** `/lrs` in the admin Next.js app  
**File:** [apps/admin/app/lrs/page.tsx](../apps/admin/app/lrs/page.tsx)  
**Collection:** [apps/admin/collections/lrs.ts](../apps/admin/collections/lrs.ts)

The page shows:
1. **Dashboard KPI cards** — total / sent / failed / dead-lettered counts.
2. **Verb breakdown** — counts per verb, failure rate, last sent timestamp.
3. **Statements table** — paginated, filterable by status and verb. Each row shows status badge, verb chip, student name, course + session date, created time, retry count.
4. **Actions** — view (opens detail dialog with full statement JSON), retry (for Failed/DeadLettered rows).
5. **Detail dialog** — all fields + pretty-printed `statementJson`.

Supported verbs listed in the filter dropdown: `registered`, `initialized`, `attended`, `completed`, `progressed`, `rated`, `earned`.

---

## Operational runbook

### Check if LRS is enabled

```bash
# In Docker environment
docker exec qalam-api printenv | grep LrsSettings__Enabled
```

### Inspect the DLQ

```bash
# Via RabbitMQ management UI (port 15672)
# Queue: lrs-statements.dlq
```

### Retry a dead-lettered statement

Use the admin UI (**LRS** sidebar entry → find the statement → Retry button), or call the API directly:

```http
POST /Api/V1/Admin/Lrs/Statements/{id}/Retry
Authorization: Bearer {admin_token}
```

### Monitor failure rate

Dashboard endpoint returns `failureRate` (0–1 float). Alert if it exceeds 0.05 (5%).

### Disable temporarily

Set `LrsSettings__Enabled=false` in the container environment and restart `qalam-api` and `qalam-messaging-api`. No statements will be queued; existing queued messages will be processed when re-enabled.

---

## Key source files

| File | Purpose |
|------|---------|
| [Qalam.Data/Entity/LRS/LrsStatementLog.cs](../Qalam.Data/Entity/LRS/LrsStatementLog.cs) | EF Core entity + status enum |
| [Qalam.Data/Helpers/LrsSettings.cs](../Qalam.Data/Helpers/LrsSettings.cs) | Config POCO |
| [Qalam.Data/DTOs/LRS/](../Qalam.Data/DTOs/LRS/) | xAPI DTOs (Statement, Actor, Verb, Object, Result, Context, Score, NelcExtensions) |
| [Qalam.Service/Implementations/LRS/LrsDispatchService.cs](../Qalam.Service/Implementations/LRS/LrsDispatchService.cs) | Builds and queues statements |
| [Qalam.Service/Implementations/LRS/XApiStatementBuilder.cs](../Qalam.Service/Implementations/LRS/XApiStatementBuilder.cs) | Constructs xAPI-compliant statement objects |
| [Qalam.Service/Implementations/LRS/NelcLrsClient.cs](../Qalam.Service/Implementations/LRS/NelcLrsClient.cs) | HTTP client (Basic auth + xAPI version header) |
| [Qalam.MessagingApi/BackgroundServices/LrsStatementConsumer.cs](../Qalam.MessagingApi/BackgroundServices/LrsStatementConsumer.cs) | RabbitMQ consumer — delivers to LRS, retries, DLQs |
| [Qalam.Core/Features/Admin/Lrs/](../Qalam.Core/Features/Admin/Lrs/) | CQRS queries + commands (dashboard, list, detail, retry) |
| [Qalam.Infrastructure/Repositories/LrsStatementLogRepository.cs](../Qalam.Infrastructure/Repositories/LrsStatementLogRepository.cs) | Read-side repository |
| [Qalam.Api/Controllers/Admin/AdminLrsController.cs](../Qalam.Api/Controllers/Admin/AdminLrsController.cs) | Admin REST API |
| [apps/admin/collections/lrs.ts](../apps/admin/collections/lrs.ts) | Fetch helpers + TypeScript types |
| [apps/admin/app/lrs/page.tsx](../apps/admin/app/lrs/page.tsx) | Admin LRS monitoring page |
