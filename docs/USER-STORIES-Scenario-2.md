# User Stories — Scenario 2: Open Session Request
## قصص المستخدم — السيناريو الثاني: طلب جلسات مفتوح

> Source of truth: codebase as of **2026-06-17**. Where BRD/role docs and code diverge, **code wins**; divergences are listed in §12.
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
| **Maturity** | **Student CRUD + broadcast/targeted matching + teacher inbox + offers + chat all implemented**; **offer-acceptance, S2 payment, admin S2 APIs, and draft wizard still planned** |
| **Architecture** | ASP.NET Core API (`Qalam.Api`), MediatR (`Qalam.Core/Features`), EF Core (`Qalam.Data/Entity`), SQL Server, RabbitMQ (attachments + notification emails). Chat is HTTP cursor-paginated — **no SignalR hub** in codebase today. |
| **Matching gate** | Broadcast matching only targets teachers whose `TeacherSubject.VerificationStatus == Approved` **and** `IsActive` (see §3, §12). Since `20260616224014_TeacherSubjectPendingByDefault`, new subjects start `Pending` → admin approval (X-008) is now a hard prerequisite for being matched. |

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
| Teacher available-requests inbox / offers | — | — | ✓ | — | — |
| Offer conversation (chat) | ✓ | ✓ | ✓ | — | — |
| Approve `TeacherSubject` (gates matching) | — | — | — | ✓ | ✓ |
| Admin S2 dashboard / disputes / reports | — | — | — | planned | planned |

Auth: `[Authorize(Roles = ...)]`. Student/Guardian endpoints carry `[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]`; teacher endpoints `[Authorize(Roles = Roles.Teacher)]`; the shared `Conversations` controller is `[Authorize]` and derives the caller's role from the JWT via the access guard. Guardian uses Student endpoints with `studentId` in body where applicable.

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

**Source:** `[code]` offers are projected inside the request detail (`GetOpenSessionRequestByIdQueryHandler`); **no dedicated student "offers list / compare" endpoint yet.**

**Endpoints:**
- `GET /Api/V1/Student/OpenSessionRequests/{id}` → `GetOpenSessionRequestByIdQueryHandler` (returns sessions, targets **and offers**)

**Status:** `partially implemented`

**Notes:** Student sees offers through the request-detail payload. A purpose-built compare/sort endpoint and the accept action (S2-ST-010) are still planned.

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
**I want** to chat with each teacher about my request,
**so that** I clarify details — even before any offer is submitted.

**Source:** `[code]` `OfferConversationsController` (`Qalam.Api/Controllers/Common`) + `GetOrCreateConversationByRequestQueryHandler`, `PostConversationMessageCommandHandler`, `GetConversationMessagesQueryHandler`, `MarkConversationReadCommandHandler`

**Acceptance criteria:**
- [ ] AC1: A conversation is keyed by **(request, teacher)** — not by offer — so the preliminary "طلب توضيح" chat can open before any offer exists and survives withdraw/re-offer cycles.
- [ ] AC2: Either party (student/guardian or teacher) calls `GET …/by-request/{requestId}/teacher/{teacherId}` to find-or-create the thread; the access guard authorizes from the JWT.
- [ ] AC3: Messages are cursor-paginated (`cursor` = ISO-8601 `SentAt`, `direction` = `older`|`newer`, `take` default 50).
- [ ] AC4: Sender is taken from the JWT on `POST …/messages`; offer updates post a `System`/`OfferUpdate` message automatically.

**Endpoints:**
- `GET /Api/V1/Conversations/by-request/{requestId:int}/teacher/{teacherId:int}` → `GetOrCreateConversationByRequestQueryHandler`
- `GET /Api/V1/Conversations/{conversationId:int}/messages` → `GetConversationMessagesQueryHandler`
- `POST /Api/V1/Conversations/{conversationId:int}/messages` → `PostConversationMessageCommandHandler`
- `POST /Api/V1/Conversations/{conversationId:int}/read` → `MarkConversationReadCommandHandler`

**Entities touched:** `OfferConversation`, `OfferMessage`

**Status:** `implemented`

**Notes:** Chat is HTTP-polled (cursor pagination); no SignalR push yet. Cross-ref S2-TE-009.

---

## 5. Teacher stories (S2-TE-xxx)

> **Teacher Scenario 2 is now implemented in code** (controllers `TeacherAvailableRequestsController`, `TeacherSessionOffersController`, and shared `OfferConversationsController`), all under `[Authorize(Roles = Roles.Teacher)]` except chat (`[Authorize]`). Only offer-acceptance follow-ups (S2-TE-010/011) remain planned.

### S2-TE-001: استقبال إشعار بطلب جديد

**As** a teacher,
**I want** to receive a notification when a matched request is published,
**so that** I can submit an offer quickly.

**Source:** `[code]` `OpenSessionRequestTargetingService.RunMatchingAndNotifyAsync` / `NotifyTargetedTeacherAsync` — queues an email per matched teacher via RabbitMQ. Invoked synchronously from `CreateOpenSessionRequestCommandHandler` when the request becomes `Active`.

**Acceptance criteria:**
- [ ] AC1: Broadcast — every teacher returned by `TeacherMatchingService.FindMatchingTeacherIdsAsync` (active, **approved** `TeacherSubject` for the subject) gets an `OpenSessionRequestTarget` row + email.
- [ ] AC2: Targeted (`targetedTeacherId`) — only that teacher is notified.
- [ ] AC3: Email only — **no in-app/push channel and no SignalR**; the inbox (S2-TE-002) is the in-app surface.

**Entities touched:** `OpenSessionRequestTarget`

**Status:** `implemented`

**Notes:** Email channel only. A real-time/push notification is not in code.

---

### S2-TE-002: عرض الطلبات المتاحة

**As** a teacher,
**I want** to browse available open session requests with filters,
**so that** I find requests matching my subjects.

**Source:** `[code]` `GetAvailableRequestsQueryHandler`

**Endpoints:**
- `GET /Api/V1/Teacher/AvailableRequests` → `GetAvailableRequestsQueryHandler` (paginated; target-status filter, `Notified`/`Viewed`/`OfferSubmitted`/`Skipped`)

**Entities touched:** `OpenSessionRequest`, `OpenSessionRequestTarget`

**Status:** `implemented`

---

### S2-TE-003: عرض تفاصيل طلب مع تطابق التوفر

**As** a teacher,
**I want** to view request details and a per-session availability match,
**so that** I decide whether to offer.

**Source:** `[code]` `GetAvailableRequestByIdQueryHandler` + `GetAvailableRequestAvailabilityMatchQueryHandler`

**Acceptance criteria:**
- [ ] AC1: Detail view flips the target row `Notified → Viewed` on first call.
- [ ] AC2: Availability-match returns, per session, one of `Available` / `Conflict` / `OutsideAvailability`.

**Endpoints:**
- `GET /Api/V1/Teacher/AvailableRequests/{id:int}` → `GetAvailableRequestByIdQueryHandler`
- `GET /Api/V1/Teacher/AvailableRequests/{id:int}/availability-match` → `GetAvailableRequestAvailabilityMatchQueryHandler`

**Status:** `implemented`

---

### S2-TE-004: تجاهل / إخفاء طلب

**As** a teacher,
**I want** to mark a request viewed or dismiss it,
**so that** my inbox reflects what I've handled.

**Source:** `[code]` `MarkAvailableRequestViewedCommandHandler` + `DismissAvailableRequestCommandHandler`

**Acceptance criteria:**
- [ ] AC1: `mark-viewed` is idempotent — acts only when target status is `Notified`.
- [ ] AC2: `dismiss` sets the target status to `Skipped` (hidden from inbox, not a formal rejection).

**Endpoints:**
- `PUT /Api/V1/Teacher/AvailableRequests/{id:int}/mark-viewed` → `MarkAvailableRequestViewedCommandHandler`
- `POST /Api/V1/Teacher/AvailableRequests/{id:int}/dismiss` → `DismissAvailableRequestCommandHandler`

**Status:** `implemented`

---

### S2-TE-005: تقديم عرض

**As** a teacher,
**I want** to submit an offer with price and terms,
**so that** the student can consider my proposal.

**Source:** `[code]` `CreateSessionOfferCommandHandler`

**Acceptance criteria:**
- [ ] AC1: `POST` returns `201` with `TeacherOfferDetailDto`; flips the target row to `OfferSubmitted`.
- [ ] AC2: A second non-`Withdrawn` offer on the same request → **409** with `meta.existingOfferId`.
- [ ] AC3: The teacher does **not** propose schedule — the student's session timings are implicit (offer carries price/notes/validity only).

**Endpoints:**
- `POST /Api/V1/Teacher/Offers` → `CreateSessionOfferCommandHandler`

**Entities touched:** `OpenSessionOffer`, `OpenSessionRequestTarget`

**Status:** `implemented`

---

### S2-TE-006: عرض عروضي

**As** a teacher,
**I want** to list my offers by status and inspect one,
**so that** I track pending and accepted offers.

**Source:** `[code]` `GetMyOffersQueryHandler` + `GetMyOfferByIdQueryHandler`

**Endpoints:**
- `GET /Api/V1/Teacher/Offers/my` → `GetMyOffersQueryHandler` (status + date-range filters, paginated)
- `GET /Api/V1/Teacher/Offers/{id:int}` → `GetMyOfferByIdQueryHandler` (with parent request snapshot)

**Status:** `implemented`

---

### S2-TE-007: تحديث عرض

**As** a teacher,
**I want** to update a pending offer,
**so that** I adjust price or message before acceptance.

**Source:** `[code]` `UpdateSessionOfferCommandHandler`

**Acceptance criteria:**
- [ ] AC1: Only price / notes / validity are editable — schedule is not.
- [ ] AC2: Each update bumps `OpenSessionOffer.Version` and posts a "تم تحديث العرض" system message into the conversation.

**Endpoints:**
- `PUT /Api/V1/Teacher/Offers/{id:int}` → `UpdateSessionOfferCommandHandler`

**Status:** `implemented`

---

### S2-TE-008: سحب عرض

**As** a teacher,
**I want** to withdraw my offer,
**so that** I am not bound to outdated terms.

**Source:** `[code]` `WithdrawSessionOfferCommandHandler`

**Acceptance criteria:**
- [ ] AC1: Withdrawing a `Pending` offer sets status `Withdrawn`; a new offer can then be submitted on the same request.

**Endpoints:**
- `POST /Api/V1/Teacher/Offers/{id:int}/withdraw` → `WithdrawSessionOfferCommandHandler`

**Status:** `implemented`

---

### S2-TE-009: التفاوض عبر الشات

**As** a teacher,
**I want** to message the student on the request thread,
**so that** we align — before or after I submit an offer.

**Source:** `[code]` shared `OfferConversationsController` (see S2-ST-012 for the full endpoint set). Conversation is keyed by **(request, teacher)**, so a teacher can chat even before offering.

**Endpoints:**
- `GET /Api/V1/Conversations/by-request/{requestId:int}/teacher/{teacherId:int}` → `GetOrCreateConversationByRequestQueryHandler`
- `POST /Api/V1/Conversations/{conversationId:int}/messages` → `PostConversationMessageCommandHandler`

**Entities touched:** `OfferConversation`, `OfferMessage`

**Status:** `implemented`

**Notes:** Cross-ref S2-ST-012. HTTP-polled, no SignalR.

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

> Sourced from `[BRD]` + `docs/ADMIN-ROLE-Scenario2.md`. **No Admin Scenario 2 dashboard/dispute/report controllers in code.** The only admin code that touches the S2 flow is `TeacherSubject` approval (X-008, `ApproveTeacherSubjectCommandHandler`), which now gates who broadcast matching can reach — see §3 and S2-TE-001.

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
| S2-ST-012, S2-TE-009 | `Qalam.Api/Controllers/Common/OfferConversationsController.cs` + `Qalam.Core/Features/Teacher/OpenSessionRequests/{Commands/PostConversationMessage,Commands/MarkConversationRead,Queries/GetConversationMessages,Queries/GetOrCreateConversationByRequest}` |
| S2-TE-001 | `Qalam.Service/Implementations/OpenSessionRequestTargetingService.cs` + `TeacherMatchingService.cs` |
| S2-TE-002–004 | `Qalam.Api/Controllers/Teacher/TeacherAvailableRequestsController.cs` + `Qalam.Core/Features/Teacher/OpenSessionRequests/{Queries/GetAvailableRequests,Queries/GetAvailableRequestById,Queries/GetAvailableRequestAvailabilityMatch,Commands/MarkAvailableRequestViewed,Commands/DismissAvailableRequest}` |
| S2-TE-005–008 | `Qalam.Api/Controllers/Teacher/TeacherSessionOffersController.cs` + `Qalam.Core/Features/Teacher/OpenSessionRequests/{Commands/CreateSessionOffer,Commands/UpdateSessionOffer,Commands/WithdrawSessionOffer,Queries/GetMyOffers,Queries/GetMyOfferById}` |
| S2-AD-* | `docs/ADMIN-ROLE-Scenario2.md` (spec only — not in code) |
| X-007 | `Qalam.MessagingApi` — `OpenSessionRequestAttachmentConsumer` |
| X-008 | `Qalam.Core/Features/Admin/TeacherSubjects/Commands/ApproveTeacherSubject/` + `Qalam.Infrastructure/Repositories/TeacherSubjectRepository.cs` (`GetActiveTeacherIdsBySubjectAsync`) |

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
| GET | `/Api/V1/Teacher/AvailableRequests` | `GetAvailableRequestsQueryHandler` | Teacher | S2-TE-002 |
| GET | `/Api/V1/Teacher/AvailableRequests/{id:int}` | `GetAvailableRequestByIdQueryHandler` | Teacher | S2-TE-003 |
| PUT | `/Api/V1/Teacher/AvailableRequests/{id:int}/mark-viewed` | `MarkAvailableRequestViewedCommandHandler` | Teacher | S2-TE-004 |
| POST | `/Api/V1/Teacher/AvailableRequests/{id:int}/dismiss` | `DismissAvailableRequestCommandHandler` | Teacher | S2-TE-004 |
| GET | `/Api/V1/Teacher/AvailableRequests/{id:int}/availability-match` | `GetAvailableRequestAvailabilityMatchQueryHandler` | Teacher | S2-TE-003 |
| POST | `/Api/V1/Teacher/Offers` | `CreateSessionOfferCommandHandler` | Teacher | S2-TE-005 |
| PUT | `/Api/V1/Teacher/Offers/{id:int}` | `UpdateSessionOfferCommandHandler` | Teacher | S2-TE-007 |
| POST | `/Api/V1/Teacher/Offers/{id:int}/withdraw` | `WithdrawSessionOfferCommandHandler` | Teacher | S2-TE-008 |
| GET | `/Api/V1/Teacher/Offers/my` | `GetMyOffersQueryHandler` | Teacher | S2-TE-006 |
| GET | `/Api/V1/Teacher/Offers/{id:int}` | `GetMyOfferByIdQueryHandler` | Teacher | S2-TE-006 |
| GET | `/Api/V1/Conversations/by-request/{requestId:int}/teacher/{teacherId:int}` | `GetOrCreateConversationByRequestQueryHandler` | Student, Guardian, Teacher | S2-ST-012, S2-TE-009 |
| GET | `/Api/V1/Conversations/{conversationId:int}/messages` | `GetConversationMessagesQueryHandler` | Student, Guardian, Teacher | S2-ST-012, S2-TE-009 |
| POST | `/Api/V1/Conversations/{conversationId:int}/messages` | `PostConversationMessageCommandHandler` | Student, Guardian, Teacher | S2-ST-012, S2-TE-009 |
| POST | `/Api/V1/Conversations/{conversationId:int}/read` | `MarkConversationReadCommandHandler` | Student, Guardian, Teacher | S2-ST-012, S2-TE-009 |

### Planned

Still absent in code — Student **accept-offer** (S2-ST-010) + dedicated offers-compare endpoint (S2-ST-009), **S2 payment** (S2-ST-011), **draft wizard** (S2-ST-008), all **Admin S2** APIs (dashboard/requests/offers/disputes/reports/matching-rules/audit — S2-AD-001–008), and a **SignalR** chat hub. See [ADMIN-ROLE-Scenario2.md](ADMIN-ROLE-Scenario2.md).

---

## 10. Entity inventory

| ENTITY | NAMESPACE | RELATED STORY IDS |
|--------|-----------|-------------------|
| `OpenSessionRequest` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001–004 |
| `OpenSessionRequestSession` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001 |
| `OpenSessionRequestSessionUnit` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001, S2-ST-001b |
| `OpenSessionRequestInvitation` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-007 |
| `OpenSessionRequestAttachment` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-005–006 |
| `OpenSessionRequestTarget` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001, S2-ST-001b, S2-TE-001–005 |
| `OpenSessionOffer` | `Qalam.Data.Entity.OpenSessionRequests` | S2-TE-005–008, S2-ST-009 (read), S2-ST-010 (planned) |
| `OfferConversation` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-012, S2-TE-009 |
| `OfferMessage` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-012, S2-TE-009 |
| `TeacherSubject` (read, approval gate) | `Qalam.Data.Entity.Teacher` | S2-ST-001b, S2-TE-001, X-008 |
| `Enrollment` | `Qalam.Data.Entity.Course` | S2-ST-011 (planned) |
| `SessionRequest` (legacy) | `Qalam.Data.Entity.Session` | — **do not use for S2** |

---

## 11. Event inventory

| EVENT NAME | TYPE | PUBLISHER | SUBSCRIBERS | STORY IDS |
|------------|------|-----------|-------------|-----------|
| Email to matched / targeted teacher | Integration (RabbitMQ) | `OpenSessionRequestTargetingService` | Email consumer | S2-ST-001, S2-ST-001b, S2-TE-001 |
| Attachment upload queued | Integration (RabbitMQ) | `UploadOpenSessionRequestAttachmentCommandHandler` | `OpenSessionRequestAttachmentConsumer` | S2-ST-005, X-007 |
| Offer-update system message | In-app (DB) | `UpdateSessionOfferCommandHandler` | Conversation participants | S2-TE-007, S2-ST-012 |
| SignalR `OfferMessageReceived` | SignalR (**planned — not in code**) | Chat hub (planned) | Teacher/Student clients | S2-ST-012, S2-TE-009 |
| `SessionRequestPublished` domain event | Integration (**planned — not in code**) | — | — | S2-TE-001 |

---

## 12. Discrepancies (BRD vs code)

| AREA | BRD / DOC SAYS | CODE SAYS | RESOLUTION |
|------|----------------|-----------|------------|
| Entity naming | `SessionRequest`, `SessionOffer` | `OpenSessionRequest`, `OpenSessionOffer` (`sr` schema) | **Code is source** |
| Request IDs | GUID + `requestNumber` | `int` identity only | **TBD** |
| Matching on create | Teachers notified immediately | Both broadcast (`RunMatchingAndNotifyAsync`) and targeted (`NotifyTargetedTeacherAsync`) run **synchronously** on create when status is `Active` | **Code is source** (implemented) |
| **Approval gate inconsistency** | — | Broadcast matching requires `TeacherSubject.VerificationStatus == Approved` (`GetActiveTeacherIdsBySubjectAsync`), but the **targeted** path's `TargetedOpenSessionRequestValidator` checks only `IsActive` — a `Pending`/`Rejected` subject can still receive a targeted request | **Code bug — align targeted validator to also require `Approved`** |
| Conversation keying | Chat tied to an offer | `OfferConversation` is keyed by **(request, teacher)** — chat opens before any offer (`SessionOfferId` nullable) | **Code is source** |
| Draft wizard | Multi-step save draft | Single `POST` publishes; `Draft` enum unused | **Planned** |
| `ReceivingOffers` status | Active marketplace phase | No handler transitions into this status (stays `Active`) | **Planned** |
| Teacher offer schedule | Teacher proposes dates in BRD | `OpenSessionOffer`: student times implicit; offer carries price/notes/validity only | **Code is source** |
| Offer acceptance | Student accepts → enrollment + payment | No accept handler; `Enrollment.SessionOfferId` FK exists but nothing creates it; `OfferAccepted`/`PaymentPending`/`Paid` statuses never reached | **Planned** |
| Legacy `session.SessionRequest` | — | Separate from S2 | **Do not conflate** |
| SignalR chat | Real-time push | HTTP cursor-paginated only; no hub | **Planned** |
| S2 payment | Real payments after offer | No S2 payment handler / no gateway | **Planned** |

---

## 13. Open questions

1. **Targeted-path approval gate:** Should `TargetedOpenSessionRequestValidator` also require `VerificationStatus == Approved` (as broadcast matching does)? Today a student can target a teacher whose subject is still `Pending`/`Rejected`. (See §12.)
2. **Offer acceptance flow:** What creates the `Enrollment` (with `SessionOfferId`) and transitions `OfferAccepted → PaymentPending → Paid`? No handler exists yet.
3. **Legacy `SessionRequest` (`session` schema):** Delete, migrate to `sr.OpenSessionRequest`, or keep?
4. **Unified `Enrollment` for S2:** Same payment flow as Scenario 1 with `EnrollmentSource.OpenSessionRequest`?
5. **Request display ID:** Human-readable `requestNumber` while keeping `int` PK?
6. **Real payment provider:** Gateway and webhook shape for S2 offer acceptance?
7. **Real-time chat:** Add a SignalR hub, or stay with HTTP cursor polling for `OfferConversation`?

---

## 14. Summary statistics

| Metric | Value |
|--------|-------|
| **S2 stories total** | **32** (Student 13 incl. 001b, Teacher 11, Admin 8) |
| S2 Student | 13 (8 implemented, 2 partial, 3 planned) |
| S2 Teacher | 11 (9 implemented, 2 planned) |
| S2 Admin | 8 (all planned) |
| **S2 implemented** | **~17 (53%)** |
| **S2 partially implemented** | **~2 (6%)** |
| **S2 planned** | **~13 (41%)** |

> Up from ~23% implemented at the 2026-06-03 revision — the whole teacher side (inbox, offers, chat) and broadcast/targeted matching shipped since.

### Top 3 risks (Scenario 2)

1. **No offer-acceptance/payment bridge** — teachers can offer and students can chat, but nothing accepts an offer, creates the `Enrollment`, or takes payment. The marketplace can't close a deal end-to-end.
2. **Targeted-path approval gap** — broadcast matching enforces `Approved` `TeacherSubject`, but the targeted validator does not (§12). With subjects now `Pending` by default, this lets unverified subjects receive targeted requests.
3. **No admin S2 surface** — no dashboard, moderation, disputes, or financial reporting for open session requests/offers.

---

_Codebase investigation 2026-06-17. Companion: [USER-STORIES-Scenarios-1-and-2.md](USER-STORIES-Scenarios-1-and-2.md)._
