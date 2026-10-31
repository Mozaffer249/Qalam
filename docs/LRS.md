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

| Trigger | Verb IRI | Method |
|---------|----------|--------|
| Enrollment activated (payment confirmed) | `http://adlnet.gov/expapi/verbs/registered` | `DispatchEnrollmentActivatedAsync` |
| Session launched (session starts) | `http://adlnet.gov/expapi/verbs/initialized` | `DispatchSessionLaunchedAsync` |
| Student marked as attended | `http://adlnet.gov/expapi/verbs/attended` | `DispatchStudentAttendedAsync` |
| Session completed | `http://adlnet.gov/expapi/verbs/completed` (activity=session) | `DispatchSessionCompletedAsync` |
| Progress updated after each session | `http://adlnet.gov/expapi/verbs/progressed` | `DispatchProgressUpdatedAsync` |
| Course fully completed | `http://adlnet.gov/expapi/verbs/completed` (activity=course) | `DispatchCourseCompletedAsync` |

### Who calls the dispatch service

| Service | Events dispatched |
|---------|------------------|
| `SessionLifecycleHelper` | session-launched, session-completed, student-attended |
| `EnrollmentCompletionService` | course-completed, progress-updated |
| `PaymentConfirmationService` | enrollment-activated (registered) |
| `AdminSessionActionService` | student-attended (admin-side mark) |
| `SetSessionAttendanceCommandHandler` | student-attended (teacher-side mark) |
| `SessionPresenceService` | student-attended (presence trigger) |

---

## xAPI statement structure

All statements use NELC-specific context extensions on the `registered` verb:

| Extension key | Value |
|---------------|-------|
| `learnerFullName` | Student full name |
| `lmsUrl` | Platform IRI (`LrsSettings.PlatformIri`) |
| `programUrl` | `{PlatformIri}/courses/{courseId}` |
| `platform` | `{ "ar-SA": PlatformNameAr, "en-US": PlatformNameEn }` |
| `learnerMobileNo` | Student phone (if present) |
| `learnerNationality` | Student nationality (if present) |

**Actor** — identified by `mbox: mailto:{email}` when the user has an email address; falls back to an `account` with `homePage` + `name: user-{id}`.

**Object IRI**:
- Course: `{PlatformIri}/course/{courseId}`
- Session: `{PlatformIri}/course/{courseId}/session/{scheduleId}`

**Result**:
- `attended`: `completion: true`, duration as `PTnM`
- `completed` (session): `completion` and `success` both = attendance was `Present`
- `progressed`: score `scaled = completedSessions/totalSessions`, `raw/min/max`
- `completed` (course): `completion: true`, `success: true`, duration as `PnD`

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
