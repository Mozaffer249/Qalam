# User Stories — Scenario 2: Open Session Request
## قصص المستخدم — السيناريو الثاني: طلب جلسات مفتوح

> Source of truth: codebase as of **2026-06-03**. Where BRD/role docs and code diverge, **code wins**; divergences are listed in §8.
>
> **Related:** [Scenario 1 — Course Enrollment](USER-STORIES-Scenarios-1-and-2.md#4-scenario-1--course-enrollment) · [Combined index](USER-STORIES-Scenarios-1-and-2.md) · [Teacher role (S2)](TEACHER-ROLE-Scenario2.md) · [Admin role (S2)](ADMIN-ROLE-Scenario2.md)

---

## Table of contents

1. [Overview](#1-overview)
2. [Glossary](#2-glossary)
3. [Roles & permissions](#3-roles--permissions)
4. [Student stories (S2-ST-xxx)](#4-student-stories-s2-st-xxx)
5. [Teacher stories (S2-TE-xxx)](#5-teacher-stories-s2-te-xxx)
6. [Admin stories (S2-AD-xxx)](#6-admin-stories-s2-ad-xxx)
7. [Cross-cutting dependencies](#7-cross-cutting-dependencies)
8. [Code references](#8-code-references)
9. [Endpoint inventory](#9-endpoint-inventory)
10. [Entity inventory](#10-entity-inventory)
11. [Event inventory](#11-event-inventory)
12. [Discrepancies (BRD vs code)](#12-discrepancies-brd-vs-code)
13. [Open questions](#13-open-questions)
14. [Summary statistics](#14-summary-statistics)

---

## 1. Overview

| Field | Value |
|-------|-------|
| **Scenario** | **2** — طلب جلسات مفتوح (Open Session Request) |
| **Flow** | Student **publishes an open session request** → matching notifies teachers → teachers **submit offers** → student accepts → pay → sessions run |
| **Maturity** | **Student CRUD slice implemented**; matching, offers, chat, admin, payment **planned** (schema + docs exist) |
| **Architecture** | ASP.NET Core API (`Qalam.Api`), MediatR (`Qalam.Core/Features`), EF Core (`Qalam.Data/Entity`), SQL Server. No SignalR hubs in codebase today. |

---

## 2. Glossary

| Term (EN) | Arabic | Code / table |
|-----------|--------|--------------|
| Open session **request** | طلب جلسات مفتوح | `OpenSessionRequest` → `sr.SessionRequests` |
| Teacher **offer** | عرض المعلم | `OpenSessionOffer` → `sr.SessionOffers` |
| Request **target** (matched teacher) | استهداف المعلم | `OpenSessionRequestTarget` |
| **Enrollment** (post-payment) | التسجيل الفعلي | `Enrollment` (`EnrollmentSource.OpenSessionRequest` — planned) |
| Mock payment (S1 pattern) | دفع تجريبي | Reuse `PayEnrollmentParticipant` pattern — **no S2 handler yet** |

> **Note:** Legacy `SessionRequest` (`session` schema) is **not** Scenario 2. Use `OpenSessionRequest` only.

---

## 3. Roles & permissions

| Capability | Student | Guardian | Teacher | Admin | SuperAdmin |
|------------|---------|----------|---------|-------|------------|
| Create / manage open session requests | ✓ | ✓ (on behalf) | — | — | — |
| Teacher offers / available requests | — | — | planned | — | — |
| Admin S2 dashboard / disputes | — | — | — | planned | planned |
| Teacher onboarding (prerequisite) | — | — | reviewed by | ✓ | ✓ |

Auth: `[Authorize(Roles = ...)]`. Guardian uses Student endpoints with `studentId` in body where applicable.

---

## 4. Student stories (S2-ST-xxx)

### S2-ST-001: إنشاء ونشر طلب جلسات مفتوح

**As** a student,
**I want** to create and publish an open session request in one step,
**so that** qualified teachers can offer to teach me.

**Source:** `[code]` `CreateOpenSessionRequestCommandHandler`

**Acceptance criteria:**
- [ ] AC1: `POST /Api/V1/Student/OpenSessionRequests` persists `OpenSessionRequest` with sessions and education FKs.
- [ ] AC2: If group invitations pending → status `PendingInvitations`; else → `Active`.
- [ ] AC3: When the request lands in `Active` (or transitions to it on the last invitation response), `IOpenSessionRequestTargetingService.RunMatchingAndNotifyAsync` runs — every qualified teacher gets an `OpenSessionRequestTarget` row + email notification. The exception is the targeted-teacher variant (S2-ST-001b) which skips broadcast.

**Endpoints:**
- `POST /Api/V1/Student/OpenSessionRequests` → `CreateOpenSessionRequestCommandHandler`

**Entities touched:** `OpenSessionRequest`, `OpenSessionRequestSession`, `OpenSessionRequestSessionUnit`, `OpenSessionRequestInvitation`, `OpenSessionRequestTarget`

**Status:** `implemented`

#### Request body samples — `POST /Api/V1/Student/OpenSessionRequests`

The command wraps the DTO under `data`. The four cases below cover every shape the handler accepts.

**`units[]` row shape (any case)** — every row must set EXACTLY ONE of `contentUnitId` / `lessonId`:

| Row | Meaning |
|---|---|
| `{ "contentUnitId": 115, "includesAllLessons": true }` | Cover every lesson inside unit 115 |
| `{ "contentUnitId": 115, "includesAllLessons": false }` (or flag omitted) | Unit 115 as a topic header — no specific lessons committed |
| `{ "lessonId": 4501 }` | Only lesson 4501. `includesAllLessons` must be `false`/omitted — single-lesson rows can't expand (400 otherwise) |

##### Case A — Broadcast (default)

No `targetedTeacherId`. Matching engine runs at publish → every qualified teacher gets a Target row + notification email.

```json
{
  "data": {
    "studentId": 5,
    "domainId": 1,
    "subjectId": 12,
    "teachingModeId": 1,
    "totalSessionsCount": 2,
    "studentNotes": "Prefers evenings.",
    "sessions": [
      { "sequenceNumber": 1, "preferredDate": "2026-06-10", "timeSlotId": 3, "durationMinutes": 60, "units": [] },
      { "sequenceNumber": 2, "preferredDate": "2026-06-12", "timeSlotId": 3, "durationMinutes": 60, "units": [] }
    ]
  }
}
```

##### Case B — Targeted teacher (S2-ST-001b)

`targetedTeacherId` set. Broadcast is **skipped**. The server validates that the teacher offers `subjectId` and that every `units[]` entry is in that teacher's `TeacherSubjectUnits`. Only that one teacher gets a Target row + notification.

This single body shows all three `units[]` shapes in different sessions.

```json
{
  "data": {
    "studentId": 5,
    "domainId": 1,
    "subjectId": 12,
    "teachingModeId": 1,
    "targetedTeacherId": 42,
    "totalSessionsCount": 3,
    "sessions": [
      { "sequenceNumber": 1, "preferredDate": "2026-06-10", "timeSlotId": 3, "durationMinutes": 60,
        "units": [ { "contentUnitId": 115, "includesAllLessons": true } ] },
      { "sequenceNumber": 2, "preferredDate": "2026-06-12", "timeSlotId": 3, "durationMinutes": 60,
        "units": [ { "contentUnitId": 116, "includesAllLessons": false } ] },
      { "sequenceNumber": 3, "preferredDate": "2026-06-14", "timeSlotId": 3, "durationMinutes": 60,
        "units": [ { "lessonId": 4501 } ] }
    ]
  }
}
```

##### Case C — Group with invitations

`invitedStudentIds` non-empty. Status lands in `PendingInvitations`; dispatch (broadcast OR targeted) waits until every invitee responds. Works with or without `targetedTeacherId`.

```json
{
  "data": {
    "studentId": 5,
    "domainId": 1,
    "subjectId": 12,
    "teachingModeId": 2,
    "groupType": "InviteOnly",
    "totalSessionsCount": 2,
    "invitedStudentIds": [ 19, 27 ],
    "sessions": [
      { "sequenceNumber": 1, "preferredDate": "2026-06-15", "timeSlotId": 4, "durationMinutes": 90, "units": [] },
      { "sequenceNumber": 2, "preferredDate": "2026-06-17", "timeSlotId": 4, "durationMinutes": 90, "units": [] }
    ]
  }
}
```

##### Case D — Quran domain

When the domain's code is `quran`, every session row **must** include `quranContentTypeId` AND `quranLevelId`. Works with broadcast or targeted (`targetedTeacherId` shown below). 400 otherwise.

| `quranContentTypeId` | Meaning | `quranLevelId` | Meaning |
|:---:|---|:---:|---|
| 1 | حفظ (Memorization) | 1 | نوراني (Noorani) |
| 2 | تلاوة (Recitation) | 2 | مبتدئ (Beginner) |
| 3 | تجويد (Tajweed) | 3 | متوسط (Intermediate) |
| | | 4 | متقدم (Advanced) |

```json
{
  "data": {
    "studentId": 5,
    "domainId": 2,
    "subjectId": 499,
    "teachingModeId": 1,
    "targetedTeacherId": 7,
    "totalSessionsCount": 2,
    "sessions": [
      { "sequenceNumber": 1, "preferredDate": "2026-06-10", "timeSlotId": 3, "durationMinutes": 60,
        "quranContentTypeId": 1, "quranLevelId": 2,
        "units": [ { "contentUnitId": 200, "includesAllLessons": true } ] },
      { "sequenceNumber": 2, "preferredDate": "2026-06-12", "timeSlotId": 3, "durationMinutes": 60,
        "quranContentTypeId": 2, "quranLevelId": 2,
        "units": [ { "lessonId": 12345 } ] }
    ]
  }
}
```

#### Field reference

| Field | Required | Notes |
|---|:---:|---|
| `data.studentId` | yes | Learner Student.Id. Caller must be that student OR their guardian. |
| `data.domainId`, `data.subjectId`, `data.teachingModeId` | yes | Existing FKs; 404 on miss. |
| `data.curriculumId`, `data.levelId`, `data.gradeId`, `data.termId` | no | Education breadcrumb when applicable. |
| `data.targetedTeacherId` | no | Optional — switches to targeted dispatch (see Case B / S2-ST-001b). |
| `data.groupType` | yes for Group teaching modes | `OpenGroup` or `InviteOnly`. |
| `data.totalSessionsCount` | yes | Must match `sessions.length` once published. |
| `data.invitedStudentIds[]` | no | Max 5. Allowed only for Group modes. |
| `data.expiresAt` | no | Defaults to `PublishedAt + 7 days`. |
| `data.studentNotes` | no | Max 1000 chars. |
| `data.sessions[].sequenceNumber` | yes | 1-indexed, sequential. |
| `data.sessions[].preferredDate`, `data.sessions[].timeSlotId`, `data.sessions[].durationMinutes` | yes | When/how long. |
| `data.sessions[].quranContentTypeId`, `data.sessions[].quranLevelId` | yes when domain is `quran` | See Case D. |
| `data.sessions[].notes` | no | Free-text. |
| `data.sessions[].units[]` | no | Empty `[]` = no content tagging. Each row: see "units[] row shape" table above. |

---

### S2-ST-001b: إنشاء طلب جلسات موجَّه لمعلم محدد

**As** a student,
**I want** to send the open session request to one specific teacher I've already chosen,
**so that** only that teacher reviews it and the broadcast matching is skipped.

**Source:** `[code]` `CreateOpenSessionRequestCommandHandler` + `TargetedOpenSessionRequestValidator` + `OpenSessionRequestTargetingService.NotifyTargetedTeacherAsync`

**Request shape** — same `POST /Api/V1/Student/OpenSessionRequests` body as S2-ST-001, plus one new optional top-level field: `data.targetedTeacherId`. See **Case B** in the S2-ST-001 "Request body samples" section above for a complete body covering all three `units[]` row shapes.

**Acceptance criteria:**
- [ ] AC1: When `targetedTeacherId` is set, the broadcast matching algorithm is **skipped**. Only one `OpenSessionRequestTarget` row is written, for that teacher.
- [ ] AC2: Only the chosen teacher gets the email notification ("طلب جلسات جديد موجَّه إليك").
- [ ] AC3: Server rejects with **404** `"المعلم المستهدف غير موجود أو غير نشط."` if the teacher doesn't exist or is not `IsActive`.
- [ ] AC4: Server rejects with **400** `"هذا المعلم لا يُدرّس المادة المطلوبة. اختر معلماً آخر أو غيّر المادة."` if the teacher has no active `TeacherSubject` row matching the requested `subjectId`.
- [ ] AC5: Per-session `units[]` rows are hard-validated against the chosen teacher's `TeacherSubjectUnits`. `contentUnitId` outside the repertoire → **400** `"Session N: contentUnitId X is outside this teacher's repertoire."`. Same for `lessonId` whose parent `ContentUnit` isn't offered.
- [ ] AC6: Every `units[]` row must set **exactly one** of `contentUnitId` or `lessonId`; both/neither → **400** `"Session N: each unit row must set exactly one of contentUnitId or lessonId."`.
- [ ] AC7: When `targetedTeacherId` is omitted (or `null`), behavior is identical to S2-ST-001 — broadcast matching runs.
- [ ] AC8: If the request lands in `PendingInvitations` first and only flips to `Active` after the last invitee responds, the targeted dispatch fires at that point (`RespondToOpenSessionRequestInvitationCommandHandler`) — the chosen teacher is still the only one notified, not the broadcast pool.
- [ ] AC9: The chat / offer flow (`OpenSessionOffer`, `OfferConversation`) is unchanged — the teacher must still post an offer before any conversation opens.
- [ ] AC10: `units[].includesAllLessons` (new) defaults to `false`. When `true`, the row means "every lesson in the unit"; when `false`, the row means "this unit as a topic header." Setting `true` together with `lessonId` → **400** `"Session N: includesAllLessons must be false when lessonId is set — single-lesson rows can't expand."` The flag is persisted on `OpenSessionRequestSessionUnit` and echoed in read responses.

**Endpoints:**
- `POST /Api/V1/Student/OpenSessionRequests` → `CreateOpenSessionRequestCommandHandler` (same endpoint as S2-ST-001 — distinguished by presence of `targetedTeacherId`)

**Entities touched:** `OpenSessionRequest.TargetedTeacherId` (new column), `OpenSessionRequestTarget` (single row instead of N), `TeacherSubject` + `TeacherSubjectUnit` (read-only validation)

**Status:** `implemented`

**Notes:** The teacher sees no difference in their inbox — the row in `OpenSessionRequestTarget` looks identical to a broadcast match. The "targeted" provenance is recorded only on the parent request (`OpenSessionRequest.TargetedTeacherId`). To filter inbox by "directly targeted to me", read `request.TargetedTeacherId == myTeacherId`.

---

### S2-ST-002: عرض قائمة طلباتي

**As** a student,
**I want** to list my open session requests filtered by status,
**so that** I track progress.

**Endpoints:**
- `GET /Api/V1/Student/OpenSessionRequests/my?status=` → `GetMyOpenSessionRequestsQueryHandler`

**Status:** `implemented`

---

### S2-ST-003: عرض تفاصيل طلب

**As** a student,
**I want** to view one open session request,
**so that** I see sessions, attachments, and invitations.

**Endpoints:**
- `GET /Api/V1/Student/OpenSessionRequests/{id}` → `GetOpenSessionRequestByIdQueryHandler`

**Status:** `implemented`

---

### S2-ST-004: إلغاء طلب

**As** a student,
**I want** to cancel my open session request,
**so that** teachers stop submitting offers.

**Source:** `[code]` `CancelOpenSessionRequestCommandHandler`

**Endpoints:**
- `POST /Api/V1/Student/OpenSessionRequests/{id}/Cancel` → `CancelOpenSessionRequestCommandHandler`

**Entities touched:** `OpenSessionRequest`, `OpenSessionOffer` (withdraw pending if any)

**Status:** `implemented`

---

### S2-ST-005: رفع مرفق للطلب

**As** a student,
**I want** to upload files to my request,
**so that** teachers understand my needs.

**Source:** `[code]` `UploadOpenSessionRequestAttachmentCommandHandler` + RabbitMQ consumer

**Endpoints:**
- `POST /Api/V1/Student/OpenSessionRequests/{id}/Attachments` → `UploadOpenSessionRequestAttachmentCommandHandler`

**Entities touched:** `OpenSessionRequestAttachment`

**Notifications:** Async OSS upload via `OpenSessionRequestAttachmentConsumer` (MessagingApi)

**Status:** `partially implemented`

---

### S2-ST-006: حذف مرفق

**As** a student,
**I want** to delete an attachment,
**so that** I remove sensitive or wrong files.

**Endpoints:**
- `DELETE /Api/V1/Student/OpenSessionRequests/{id}/Attachments/{attachmentId}` → `DeleteOpenSessionRequestAttachmentCommandHandler`

**Status:** `implemented`

---

### S2-ST-007: الرد على دعوة مجموعة (S2)

**As** an invited student,
**I want** to accept or reject an open-session group invite,
**so that** the request can become active.

**Endpoints:**
- `POST /Api/V1/Student/OpenSessionRequests/{openSessionRequestId}/Members/Response` → `RespondToOpenSessionRequestInvitationCommandHandler`

**Status:** `implemented`

**Notes:** P3 TODO: trigger matching when status flips to `Active`.

---

### S2-ST-008: حفظ مسودة طلب (Wizard)

**As** a student,
**I want** to save a draft before publishing,
**so that** I complete the wizard over multiple steps.

**Source:** `[planned]` `OpenSessionRequestStatus.Draft` exists; create handler never sets Draft.

**Status:** `planned`

---

### S2-ST-009: عرض العروض الواردة

**As** a student,
**I want** to list teacher offers on my request,
**so that** I compare and choose.

**Source:** `[planned]` `docs/TEACHER-ROLE-Scenario2.md`; no student offers endpoint.

**Status:** `planned`

---

### S2-ST-010: قبول عرض

**As** a student,
**I want** to accept one teacher offer,
**so that** other offers are auto-rejected and I proceed to payment.

**Source:** `[planned]` Status `OfferAccepted` in enum; no handler.

**Status:** `planned`

**Notes:** Depends on S2-TE-005.

---

### S2-ST-011: الدفع بعد قبول العرض

**As** a student,
**I want** to pay after accepting an offer,
**so that** sessions are scheduled.

**Source:** `[planned]` `Enrollment` has `SessionOfferId` FK; no S2 payment handler.

**Status:** `planned`

---

### S2-ST-012: محادثة مع المعلم قبل القبول

**As** a student,
**I want** to chat with each offering teacher,
**so that** I clarify details before accepting.

**Source:** `[planned]` `OfferConversation`, `OfferMessage` tables; `docs/TEACHER-ROLE-Scenario2.md`

**Endpoints (planned):**
- `GET /Api/V1/Conversations/by-offer/{offerId}`
- `POST /Api/V1/Conversations/{conversationId}/messages`

**Status:** `planned`

---

## 5. Teacher stories (S2-TE-xxx)

> All stories below sourced from `[BRD]` + `docs/TEACHER-ROLE-Scenario2.md`. **No Teacher Scenario 2 controllers exist in code.**

### S2-TE-001: استقبال إشعار بطلب جديد

**As** a teacher,
**I want** to receive a notification when a matched request is published,
**so that** I can submit an offer quickly.

**Source:** `[planned]` Event `SessionRequestPublished` in docs; no publisher in code.

**Status:** `planned`

---

### S2-TE-002: عرض الطلبات المتاحة

**As** a teacher,
**I want** to browse available open session requests with filters,
**so that** I find requests matching my subjects.

**Endpoints (planned):**
- `GET /Api/V1/Teacher/AvailableRequests?status=new&page=1&pageSize=20` → planned handler

**Entities touched (planned):** `OpenSessionRequest`, `OpenSessionRequestTarget`

**Status:** `planned`

---

### S2-TE-003: عرض تفاصيل طلب مع تطابق التوفر

**As** a teacher,
**I want** to view request details and availability match score,
**so that** I decide whether to offer.

**Endpoints (planned):**
- `GET /Api/V1/Teacher/AvailableRequests/{id}`
- `GET /Api/V1/Teacher/AvailableRequests/{id}/availability-match`

**Status:** `planned`

---

### S2-TE-004: تجاهل / إخفاء طلب

**As** a teacher,
**I want** to dismiss a request,
**so that** it leaves my inbox.

**Endpoints (planned):**
- `PUT /Api/V1/Teacher/AvailableRequests/{id}/mark-viewed`
- `POST /Api/V1/Teacher/AvailableRequests/{id}/dismiss`

**Status:** `planned`

---

### S2-TE-005: تقديم عرض

**As** a teacher,
**I want** to submit an offer with price and terms,
**so that** the student can consider my proposal.

**Endpoints (planned):**
- `POST /Api/V1/Teacher/Offers` → planned `CreateOpenSessionOfferCommandHandler`

**Entities touched:** `OpenSessionOffer`

**Status:** `planned`

**Notes:** Code comment on `OpenSessionOffer`: teacher does **not** propose alternate schedule — student sessions are implicit.

---

### S2-TE-006: عرض عروضي

**As** a teacher,
**I want** to list my offers by status,
**so that** I track pending and accepted offers.

**Endpoints (planned):**
- `GET /Api/V1/Teacher/Offers/my?status=pending`

**Status:** `planned`

---

### S2-TE-007: تحديث عرض

**As** a teacher,
**I want** to update a pending offer,
**so that** I adjust price or message before acceptance.

**Endpoints (planned):**
- `PUT /Api/V1/Teacher/Offers/{id}`

**Status:** `planned`

---

### S2-TE-008: سحب عرض

**As** a teacher,
**I want** to withdraw my offer,
**so that** I am not bound to outdated terms.

**Endpoints (planned):**
- `POST /Api/V1/Teacher/Offers/{id}/withdraw`

**Status:** `planned`

---

### S2-TE-009: التفاوض عبر الشات

**As** a teacher,
**I want** to message the student on an offer thread,
**so that** we align before acceptance.

**Source:** `[planned]` `docs/TEACHER-ROLE-Scenario2.md` § Screen T-5

**Status:** `planned`

**Notes:** Cross-ref S2-ST-012.

---

### S2-TE-010: إشعار بقبول العرض

**As** a teacher,
**I want** to be notified when my offer is accepted,
**so that** I prepare for sessions.

**Status:** `planned`

---

### S2-TE-011: تنفيذ الجلسات المجدولة (S2)

**As** a teacher,
**I want** to conduct scheduled sessions after payment,
**so that** I deliver the requested teaching.

**Status:** `planned`

---

## 6. Admin stories (S2-AD-xxx)

> Sourced from `[BRD]` + `docs/ADMIN-ROLE-Scenario2.md`. **No Admin Scenario 2 controllers in code.**

### S2-AD-001: لوحة مؤشرات Scenario 2

**As** an admin,
**I want** dashboard KPIs for open session requests,
**so that** I monitor conversion and revenue.

**Endpoints (planned):**
- `GET /Api/V1/Admin/Dashboard/kpis`
- `GET /Api/V1/Admin/Dashboard/charts`
- `GET /Api/V1/Admin/Dashboard/recent-activity`

**Status:** `planned`

---

### S2-AD-002: إدارة الطلبات (قائمة وتفاصيل)

**As** an admin,
**I want** to list and inspect session requests,
**so that** I support users and enforce policy.

**Endpoints (planned):**
- `GET /Api/V1/Admin/SessionRequests`
- `GET /Api/V1/Admin/SessionRequests/{id}`
- `GET /Api/V1/Admin/SessionRequests/{id}/timeline`

**Status:** `planned`

---

### S2-AD-003: تعليق / إعادة تفعيل / تعديل طلب

**As** an admin,
**I want** to suspend, reactivate, or admin-edit a request,
**so that** I resolve abuse or errors.

**Endpoints (planned):**
- `POST /Api/V1/Admin/SessionRequests/{id}/suspend`
- `POST /Api/V1/Admin/SessionRequests/{id}/reactivate`
- `PUT /Api/V1/Admin/SessionRequests/{id}/admin-edit`

**Status:** `planned`

---

### S2-AD-004: إدارة العروض

**As** an admin,
**I want** to list offers and force-withdraw when needed,
**so that** I moderate marketplace behavior.

**Endpoints (planned):**
- `GET /Api/V1/Admin/Offers`
- `POST /Api/V1/Admin/Offers/{id}/force-withdraw`

**Status:** `planned`

---

### S2-AD-005: حل النزاعات

**As** an admin,
**I want** to manage disputes with optional chat access,
**so that** conflicts are resolved fairly.

**Endpoints (planned):**
- `GET /Api/V1/Admin/Disputes`
- `POST /Api/V1/Admin/Disputes/{id}/resolve`
- `POST /Api/V1/Admin/Disputes/{id}/access-chat`

**Status:** `planned`

---

### S2-AD-006: تقارير مالية Scenario 2

**As** an admin,
**I want** revenue and teacher performance reports with export,
**so that** leadership has data.

**Endpoints (planned):**
- `GET /Api/V1/Admin/Reports/revenue`
- `POST /Api/V1/Admin/Reports/export`

**Status:** `planned`

---

### S2-AD-007: قواعد المطابقة

**As** an admin,
**I want** to configure matching rules and teacher exclusions,
**so that** request routing is controlled.

**Endpoints (planned):**
- `GET /Api/V1/Admin/MatchingRules`
- `PUT /Api/V1/Admin/MatchingRules`
- `POST /Api/V1/Admin/MatchingRules/exclude-teacher`

**Status:** `planned`

---

### S2-AD-008: سجل التدقيق

**As** an admin,
**I want** to query the audit log,
**so that** I investigate admin and system actions.

**Endpoints (planned):**
- `GET /Api/V1/Admin/AuditLog`

**Status:** `planned`

**Notes:** `AuditLoggingMiddleware` logs auth actions to DB today; dedicated admin audit API is planned.

---

---

## 7. Cross-cutting dependencies

Stories below live in the combined doc or other guides but are required for Scenario 2.

| ID | Story | Endpoints | Status |
|----|-------|-----------|--------|
| X-001 | Public auth config | `GET /Api/V1/Authentication/Config` | implemented |
| X-003 | Student registration (OTP) | `POST /Api/V1/Authentication/Student/*` | implemented |
| X-004 | JWT refresh | `POST /Api/V1/Authentication/RefreshToken` | implemented |
| X-007 | OSS attachment upload | `OpenSessionRequestAttachmentConsumer` | partially implemented |
| X-008 | Teacher KYC review (prerequisite for matching) | `GET/POST /Api/V1/Admin/TeacherManagement/*` | implemented |

See [USER-STORIES-Scenarios-1-and-2.md §6](USER-STORIES-Scenarios-1-and-2.md#6-shared-cross-cutting-stories-x-xxx) for full cross-cutting stories.

---

## 8. Code references

| Story ID | Primary code file(s) |
|----------|---------------------|
| S2-ST-001 | `Qalam.Core/Features/Student/OpenSessionRequests/Commands/CreateOpenSessionRequest/CreateOpenSessionRequestCommandHandler.cs` |
| S2-ST-001b | `TargetedOpenSessionRequestValidator` + `OpenSessionRequestTargetingService` |
| S2-ST-004 | `Qalam.Core/Features/Student/OpenSessionRequests/Commands/CancelOpenSessionRequest/` |
| S2-ST-005 | `Qalam.Core/Features/Student/OpenSessionRequests/Commands/UploadOpenSessionRequestAttachment/` |
| S2-ST-007 | `Qalam.Core/Features/Student/OpenSessionRequests/Commands/RespondToOpenSessionRequestInvitation/` |
| S2-TE-* | `docs/TEACHER-ROLE-Scenario2.md` (spec only) |
| S2-AD-* | `docs/ADMIN-ROLE-Scenario2.md` (spec only) |
| X-007 | `Qalam.MessagingApi` — `OpenSessionRequestAttachmentConsumer` |

---

## 9. Endpoint inventory

### Implemented

| METHOD | PATH | HANDLER | ROLE | STORY ID(S) |
|--------|------|---------|------|-------------|
| POST | `/Api/V1/Student/OpenSessionRequests` | `CreateOpenSessionRequestCommandHandler` | Student, Guardian | S2-ST-001, S2-ST-001b |
| GET | `/Api/V1/Student/OpenSessionRequests/my` | `GetMyOpenSessionRequestsQueryHandler` | Student, Guardian | S2-ST-002 |
| GET | `/Api/V1/Student/OpenSessionRequests/{id}` | `GetOpenSessionRequestByIdQueryHandler` | Student, Guardian | S2-ST-003 |
| POST | `/Api/V1/Student/OpenSessionRequests/{id}/Cancel` | `CancelOpenSessionRequestCommandHandler` | Student, Guardian | S2-ST-004 |
| POST | `/Api/V1/Student/OpenSessionRequests/{id}/Attachments` | `UploadOpenSessionRequestAttachmentCommandHandler` | Student, Guardian | S2-ST-005 |
| DELETE | `/Api/V1/Student/OpenSessionRequests/{id}/Attachments/{attachmentId}` | `DeleteOpenSessionRequestAttachmentCommandHandler` | Student, Guardian | S2-ST-006 |
| POST | `/Api/V1/Student/OpenSessionRequests/{openSessionRequestId}/Members/Response` | `RespondToOpenSessionRequestInvitationCommandHandler` | Student, Guardian | S2-ST-007 |

### Planned

Documented in [TEACHER-ROLE-Scenario2.md](TEACHER-ROLE-Scenario2.md) and [ADMIN-ROLE-Scenario2.md](ADMIN-ROLE-Scenario2.md) — Teacher `AvailableRequests` / `Offers`, Admin `SessionRequests` / `Disputes`, Conversations API, offer accept + S2 payment.

---

## 10. Entity inventory

| ENTITY | NAMESPACE | RELATED STORY IDS |
|--------|-----------|-------------------|
| `OpenSessionRequest` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001–004 |
| `OpenSessionRequestSession` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001 |
| `OpenSessionRequestSessionUnit` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001, S2-ST-001b |
| `OpenSessionRequestInvitation` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-007 |
| `OpenSessionRequestAttachment` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-005–006 |
| `OpenSessionRequestTarget` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001, S2-TE-002 |
| `OpenSessionOffer` | `Qalam.Data.Entity.OpenSessionRequests` | S2-TE-005–008, S2-ST-009–010 |
| `OfferConversation` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-012, S2-TE-009 |
| `OfferMessage` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-012, S2-TE-009 |
| `Enrollment` | `Qalam.Data.Entity.Course` | S2-ST-011 (planned) |
| `SessionRequest` (legacy) | `Qalam.Data.Entity.Session` | — **do not use for S2** |

---

## 11. Event inventory

| EVENT NAME | TYPE | PUBLISHER | SUBSCRIBERS | STORY IDS |
|------------|------|-----------|-------------|-----------|
| `SessionRequestPublished` | Integration (planned) | Matching engine (planned) | Notification service (planned) | S2-TE-001 |
| SignalR `OfferMessageReceived` | SignalR (planned) | Chat hub (planned) | Teacher/Student clients | S2-ST-012, S2-TE-009 |
| Attachment upload queued | Integration | Upload handler | `OpenSessionRequestAttachmentConsumer` | S2-ST-005, X-007 |
| Email to matched / targeted teacher | Integration | `OpenSessionRequestTargetingService` | Email consumer | S2-ST-001, S2-ST-001b |

---

## 12. Discrepancies (BRD vs code)

| AREA | BRD / DOC SAYS | CODE SAYS | RESOLUTION |
|------|----------------|-----------|------------|
| Entity naming | `SessionRequest`, `SessionOffer` | `OpenSessionRequest`, `OpenSessionOffer` (`sr` schema) | **Code is source** |
| Request IDs | GUID + `requestNumber` | `int` identity only | **TBD** |
| Matching on create | Teachers notified immediately | `P3` TODO in some paths; targeted flow implemented | **Verify per release** |
| Draft wizard | Multi-step save draft | Single `POST` publishes; `Draft` enum unused | **Planned** |
| `ReceivingOffers` status | Active marketplace phase | No handler transitions into this status | **Planned** |
| Teacher offer schedule | Teacher proposes dates in BRD | `OpenSessionOffer`: student times implicit | **TBD** with product |
| Legacy `session.SessionRequest` | — | Separate from S2 | **Do not conflate** |
| SignalR chat | Full conversation API | No hub, no controller | **Planned** |
| S2 payment | Real payments after offer | No S2 payment handler | **Planned** |

---

## 13. Open questions

1. **Legacy `SessionRequest` (`session` schema):** Delete, migrate to `sr.OpenSessionRequest`, or keep?
2. **Matching engine (P3):** Sync on publish, background job, or admin rules first?
3. **Unified `Enrollment` for S2:** Same payment flow as Scenario 1 with `EnrollmentSource.OpenSessionRequest`?
4. **Request display ID:** Human-readable `requestNumber` while keeping `int` PK?
5. **Real payment provider:** Gateway and webhook shape for S2 offer acceptance?

---

## 14. Summary statistics

| Metric | Value |
|--------|-------|
| **S2 stories total** | **31** |
| S2 Student | 12 (7 implemented, 1 partial, 4 planned) |
| S2 Teacher | 11 (all planned) |
| S2 Admin | 8 (all planned) |
| **S2 implemented** | **~7 (23%)** |
| **S2 partially implemented** | **~1 (3%)** |
| **S2 planned** | **~23 (74%)** |

### Top 3 risks (Scenario 2)

1. **Matching / offers gap** — student publish works; teacher inbox and offer APIs not shipped.
2. **Dual session-request models** (`session.SessionRequest` vs `sr.OpenSessionRequest`) causing developer confusion.
3. **No admin or financial APIs** for disputes, reports, and marketplace moderation.

---

_Extracted from [USER-STORIES-Scenarios-1-and-2.md](USER-STORIES-Scenarios-1-and-2.md); codebase investigation 2026-06-03._
