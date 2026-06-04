# User Stories — Scenarios 1 & 2
## قصص المستخدم الكاملة — السيناريو الأول والثاني

> Source of truth: codebase as of **2026-06-03**. Where BRD/role docs and code diverge, **code wins**; divergences are listed in §11.

---

## Table of contents

1. [Overview](#1-overview)
2. [Glossary](#2-glossary)
3. [Roles & permissions matrix](#3-roles--permissions-matrix)
4. [Scenario 1 — Course Enrollment](#4-scenario-1--course-enrollment)
5. [Scenario 2 — Open Session Request](#5-scenario-2--open-session-request)
6. [Shared cross-cutting stories](#6-shared-cross-cutting-stories)
7. [Code references — quick index](#7-code-references--quick-index)
8. [Endpoint inventory](#8-endpoint-inventory)
9. [Entity inventory](#9-entity-inventory)
10. [Event inventory](#10-event-inventory)
11. [Discrepancies between BRD and code](#11-discrepancies-between-brd-and-code)
12. [Open questions](#12-open-questions)

---

## 1. Overview

Qalam supports two commercial learning flows on one platform:

| Scenario | Arabic name | Flow summary | Implementation maturity |
|----------|-------------|--------------|-------------------------|
| **1** | تسجيل الدورة | Student browses a **published course** → submits **enrollment request** (individual or group) → teacher approves (if flexible course) → **mock payment** → **Enrollment** + **CourseSchedule** generated | **Student + Teacher APIs largely implemented**; Admin course ops missing; no live session/assignment APIs |
| **2** | طلب جلسات مفتوح | Student **publishes an open session request** → matching notifies teachers → teachers **submit offers** → student accepts → pay → sessions run | **Student CRUD slice implemented**; matching, offers, chat, admin, payment **planned** (schema + docs exist) |

**Architecture:** ASP.NET Core API (`Qalam.Api`), MediatR handlers (`Qalam.Core/Features`), EF Core entities (`Qalam.Data/Entity`), SQL Server. No SignalR hubs or Hangfire in the current codebase.

---

## 2. Glossary

| Term (EN) | Arabic | Code / table |
|-----------|--------|--------------|
| Course enrollment **request** | طلب التسجيل في دورة | `CourseEnrollmentRequest` |
| **Enrollment** (post-approval) | التسجيل الفعلي | `Enrollment` (`EnrollmentSource.CourseRequest`) |
| Course outline session | جلسة في منهج الدورة | `CourseSession` |
| Scheduled session (calendar) | جلسة مجدولة للطالب | `CourseSchedule` |
| Open session **request** | طلب جلسات مفتوح | `OpenSessionRequest` → `sr.SessionRequests` |
| Teacher **offer** | عرض المعلم | `OpenSessionOffer` → `sr.SessionOffers` |
| Mock payment | دفع تجريبي | `PayEnrollmentParticipantCommandHandler` (always succeeds) |

> **Note:** There is **no** `CourseEnrollment` entity. Post-approval state is always `Enrollment`.

---

## 3. Roles & permissions matrix

| Capability | Student | Guardian | Teacher | Admin | SuperAdmin |
|------------|---------|----------|---------|-------|------------|
| Browse published courses | ✓ | ✓ | — | — | — |
| Enroll / pay (self or child) | ✓ (self) | ✓ (child) | — | — | — |
| Manage own courses | — | — | ✓ | — | — |
| Approve/reject enrollment requests | — | — | ✓ | — | — |
| Open session request (S2) | ✓ | ✓ (on behalf) | — | — | — |
| Teacher offers / available requests (S2) | — | — | planned | — | — |
| Admin S2 dashboard / disputes | — | — | — | planned | planned |
| Teacher onboarding review | — | — | — | ✓ | ✓ |
| Auth system settings | — | — | — | — | ✓ |

Auth pattern: `[Authorize(Roles = ...)]` on controllers. Guardian uses same endpoints as Student with `studentId` in body where applicable.

---

## 4. Scenario 1 — Course Enrollment

### 4.1 Student stories (S1-ST-xxx)

### S1-ST-001: تصفح الدورات المنشورة

**As** a student,
**I want** to browse published courses with filters and pagination,
**so that** I can find a course to enroll in.

**Source:** `[code]` `Qalam.Core/Features/Student/CourseCatalog/Queries/GetPublishedCoursesList/`

**Acceptance criteria:**
- [ ] AC1: `GET /Api/V1/Student/Courses` returns only courses in published status with HTTP 200.
- [ ] AC2: Response includes teacher summary and average review when available.
- [ ] AC3: Unauthenticated caller receives HTTP 401.

**Endpoints:**
- `GET /Api/V1/Student/Courses` → `GetPublishedCoursesListQueryHandler`

**Entities touched:** `Course`, `TeacherReview` (read aggregate)

**Events fired:** —

**Notifications:** —

**Permissions:** `[Authorize(Roles = Student,Guardian)]`

**Status:** `implemented`

---

### S1-ST-002: عرض الدورات المقترحة

**As** a student,
**I want** to see a short list of recommended courses,
**so that** I can discover relevant offerings quickly.

**Source:** `[code]` `GetRecommendedCoursesQueryHandler`

**Acceptance criteria:**
- [ ] AC1: `GET /Api/V1/Student/Courses/Recommended` returns up to four published courses.
- [ ] AC2: HTTP 200 for authenticated student/guardian.

**Endpoints:**
- `GET /Api/V1/Student/Courses/Recommended` → `GetRecommendedCoursesQueryHandler`

**Entities touched:** `Course`

**Status:** `implemented`

---

### S1-ST-003: عرض تفاصيل دورة

**As** a student,
**I want** to view full course details including outline sessions,
**so that** I can decide whether to enroll.

**Source:** `[code]` `GetPublishedCourseByIdQueryHandler`

**Acceptance criteria:**
- [ ] AC1: `GET /Api/V1/Student/Courses/{id}` returns course, sessions, pricing, and teacher info for a published course.
- [ ] AC2: Non-published or missing course returns HTTP 404.

**Endpoints:**
- `GET /Api/V1/Student/Courses/{id}` → `GetPublishedCourseByIdQueryHandler`

**Entities touched:** `Course`, `CourseSession`

**Status:** `implemented`

---

### S1-ST-004: تقديم طلب تسجيل (فردي)

**As** a student,
**I want** to submit an enrollment request for a course,
**so that** the teacher can approve and I can pay.

**Source:** `[code]` `RequestCourseEnrollmentCommandHandler`

**Acceptance criteria:**
- [ ] AC1: `POST /Api/V1/Student/EnrollmentRequests` creates `CourseEnrollmentRequest` with status appropriate to course type (fixed courses may auto-approve).
- [ ] AC2: When approved path reached, an `Enrollment` in `PendingPayment` is created.
- [ ] AC3: Invalid course or duplicate active request returns HTTP 400.

**Endpoints:**
- `POST /Api/V1/Student/EnrollmentRequests` → `RequestCourseEnrollmentCommandHandler`

**Entities touched:** `CourseEnrollmentRequest`, `Enrollment`, `CourseRequestSelectedAvailability`, `CourseRequestProposedSession`

**Notifications:** Email OTP / queued mail for invites (group path)

**Status:** `implemented`

**Notes:** Flexible vs fixed course behavior differs in handler; see `EnrollmentApprovalService`.

---

### S1-ST-005: تقديم طلب تسجيل جماعي مع دعوات

**As** a student,
**I want** to invite co-students to a group enrollment request,
**so that** we enroll together as a group.

**Source:** `[code]` `RequestCourseEnrollmentCommandHandler` (group members in command)

**Acceptance criteria:**
- [ ] AC1: Request creates `CourseRequestGroupMember` rows with `Pending` invitation status.
- [ ] AC2: Request stays pending until all invitees respond (when group flow requires it).

**Entities touched:** `CourseRequestGroupMember`, `CourseEnrollmentRequest`

**Status:** `implemented`

---

### S1-ST-006: الرد على دعوة تسجيل جماعي

**As** an invited student,
**I want** to accept or reject a group enrollment invite,
**so that** the group request can proceed.

**Source:** `[code]` `RespondToGroupEnrollmentInviteCommandHandler`

**Acceptance criteria:**
- [ ] AC1: `POST /Api/V1/Student/EnrollmentRequests/{enrollmentRequestId}/Members/Response` updates member status.
- [ ] AC2: When all members accept, enrollment approval flow continues.

**Endpoints:**
- `POST /Api/V1/Student/EnrollmentRequests/{enrollmentRequestId}/Members/Response` → `RespondToGroupEnrollmentInviteCommandHandler`

**Entities touched:** `CourseRequestGroupMember`

**Status:** `implemented`

---

### S1-ST-007: عرض طلبات التسجيل الخاصة بي

**As** a student,
**I want** to list my enrollment requests,
**so that** I can track approval status.

**Source:** `[code]` `GetMyEnrollmentRequestsQueryHandler`

**Acceptance criteria:**
- [ ] AC1: `GET /Api/V1/Student/EnrollmentRequests` returns caller's requests (or guardian's child scope).
- [ ] AC2: HTTP 200 with paginated list.

**Endpoints:**
- `GET /Api/V1/Student/EnrollmentRequests` → `GetMyEnrollmentRequestsQueryHandler`

**Status:** `implemented`

---

### S1-ST-008: عرض تفاصيل طلب تسجيل

**As** a student,
**I want** to view one enrollment request in detail,
**so that** I see members, slots, and status.

**Source:** `[code]` `GetMyEnrollmentRequestByIdQueryHandler`

**Endpoints:**
- `GET /Api/V1/Student/EnrollmentRequests/{id}` → `GetMyEnrollmentRequestByIdQueryHandler`

**Status:** `implemented`

---

### S1-ST-009: عرض توفر المعلم لاختيار المواعيد

**As** a student,
**I want** to see a teacher's availability for a date range,
**so that** I can pick slots when enrolling in a flexible course.

**Source:** `[code]` `GetTeacherAvailabilityByRangeQueryHandler`

**Endpoints:**
- `GET /Api/V1/Student/Teachers/{teacherId}/Availability` → `GetTeacherAvailabilityByRangeQueryHandler`

**Entities touched:** `TeacherAvailability`

**Status:** `implemented`

---

### S1-ST-010: دفع حصة مشارك (Mock)

**As** a student or guardian,
**I want** to pay my share of an enrollment,
**so that** the enrollment becomes active and sessions are scheduled.

**Source:** `[code]` `PayEnrollmentParticipantCommandHandler`

**Acceptance criteria:**
- [ ] AC1: `POST /Api/V1/Student/Payments/Participants` with valid `enrollmentParticipantId` returns HTTP 200 and `Payment` succeeded.
- [ ] AC2: Mock gateway always succeeds; `PaymentStatus` = completed.
- [ ] AC3: When **last** pending participant pays, `Enrollment.Status` → `Active` and `CourseSchedule` rows are generated.
- [ ] AC4: Guardian can pay for linked minor; adult student can only pay self.

**Endpoints:**
- `POST /Api/V1/Student/Payments/Participants` → `PayEnrollmentParticipantCommandHandler`

**Entities touched:** `Payment`, `PaymentItem`, `EnrollmentPayment`, `EnrollmentParticipant`, `CourseSchedule`

**Status:** `implemented`

**Notes:** Real payment gateway not integrated.

---

### S1-ST-011: عرض ملخص الدفع للتسجيل

**As** a student,
**I want** to see payment breakdown for an enrollment,
**so that** I know who paid and what remains.

**Source:** `[code]` `GetEnrollmentPaymentSummaryQueryHandler`

**Endpoints:**
- `GET /Api/V1/Student/Payments/Enrollments/{enrollmentId}/Summary` → `GetEnrollmentPaymentSummaryQueryHandler`

**Status:** `implemented`

---

### S1-ST-012: عرض تسجيلاتي النشطة

**As** a student,
**I want** to list my enrollments,
**so that** I can see active and completed courses.

**Source:** `[code]` `GetMyEnrollmentsQueryHandler`

**Endpoints:**
- `GET /Api/V1/Student/Enrollments` → `GetMyEnrollmentsQueryHandler`

**Entities touched:** `Enrollment`

**Status:** `implemented`

---

### S1-ST-013: عرض تفاصيل تسجيل وجلسات مجدولة

**As** a student,
**I want** to view enrollment detail including scheduled sessions,
**so that** I know when sessions occur and whether I can start.

**Source:** `[code]` `GetMyEnrollmentByIdQueryHandler`

**Acceptance criteria:**
- [ ] AC1: Response includes `sessions[]` from `CourseSchedule`.
- [ ] AC2: `canStart` flag computed from UTC window (read-only; no start/join API).

**Endpoints:**
- `GET /Api/V1/Student/Enrollments/{id}` → `GetMyEnrollmentByIdQueryHandler`

**Status:** `implemented`

**Notes:** No endpoint to actually start/join Zoom session — see S1-ST-016.

---

### S1-ST-014: عرض الدعوات المعلقة

**As** a student,
**I want** to see pending group invitations,
**so that** I can respond to them.

**Source:** `[code]` `GetMyInvitationsQueryHandler`

**Endpoints:**
- `GET /Api/V1/Student/Invitations` → `GetMyInvitationsQueryHandler`

**Status:** `implemented`

---

### S1-ST-015: البحث عن طلاب للمجموعة

**As** a student,
**I want** to search students by name or email,
**so that** I can invite them to a group enrollment.

**Source:** `[code]` `SearchStudentsForGroupQueryHandler`, `SearchStudentsQueryHandler`

**Endpoints:**
- `GET /Api/V1/Student/Students/Search` → `SearchStudentsForGroupQueryHandler`
- `GET /Api/V1/Student/Search` → `SearchStudentsQueryHandler`

**Status:** `implemented`

---

### S1-ST-016: حضور جلسة مباشرة (Zoom)

**As** a student,
**I want** to join a live session via meeting link,
**so that** I attend the class.

**Source:** `[planned]` BRD / product docs

**Acceptance criteria:**
- [ ] AC1: Enrollment detail or session endpoint exposes join URL during session window.
- [ ] AC2: Unauthorized user cannot obtain link.

**Endpoints:** — (not implemented)

**Status:** `planned`

**Notes:** `canStart` exists on DTO only; no Zoom integration endpoints in `Qalam.Api`.

---

### S1-ST-017: تقييم المعلم بعد الجلسة

**As** a student,
**I want** to submit a review for a teacher,
**so that** other students see quality signals.

**Source:** `[planned]` Entity `TeacherReview` exists; catalog reads aggregate only.

**Status:** `planned`

---

### S1-ST-018: إلغاء التسجيل ضمن السياسة

**As** a student,
**I want** to cancel my enrollment within policy rules,
**so that** I am not charged for unwanted courses.

**Source:** `[partial]` `EnrollmentExpirationService` cancels unpaid enrollments after deadline only.

**Acceptance criteria:**
- [ ] AC1: User-initiated cancel API — **not implemented**.
- [ ] AC2: Background job sets `Enrollment.Status` = `Cancelled` when payment deadline passes.

**Status:** `partially implemented`

---

### 4.2 Teacher stories (S1-TE-xxx)

### S1-TE-001: عرض دوراتي

**As** a teacher,
**I want** to list my courses with filters,
**so that** I manage my catalog.

**Source:** `[code]` `GetCoursesListQueryHandler`

**Endpoints:**
- `GET /Api/V1/Teacher/TeacherCourse` → `GetCoursesListQueryHandler`

**Permissions:** `[Authorize(Roles = Teacher)]`

**Status:** `implemented`

---

### S1-TE-002: عرض تفاصيل دورة

**As** a teacher,
**I want** to view my course by id,
**so that** I edit or publish it.

**Endpoints:**
- `GET /Api/V1/Teacher/TeacherCourse/{id}` → `GetCourseByIdQueryHandler`

**Status:** `implemented`

---

### S1-TE-003: إنشاء دورة

**As** a teacher,
**I want** to create a course (fixed or flexible content),
**so that** students can discover and enroll.

**Source:** `[code]` `CreateCourseCommandHandler`

**Endpoints:**
- `POST /Api/V1/Teacher/TeacherCourse` → `CreateCourseCommandHandler`

**Entities touched:** `Course`, `CourseSession` (embedded in create DTO when provided)

**Status:** `implemented`

---

### S1-TE-004: تحديث دورة

**As** a teacher,
**I want** to update course metadata and content,
**so that** the listing stays accurate.

**Endpoints:**
- `PUT /Api/V1/Teacher/TeacherCourse/{id}` → `UpdateCourseCommandHandler`

**Status:** `implemented`

**Notes:** Separate session CRUD endpoints referenced in comments but **not implemented**.

---

### S1-TE-005: حذف دورة

**As** a teacher,
**I want** to delete a draft or unused course,
**so that** my catalog stays clean.

**Endpoints:**
- `DELETE /Api/V1/Teacher/TeacherCourse/{id}` → `DeleteCourseCommandHandler`

**Status:** `implemented`

---

### S1-TE-006: عرض طلبات التسجيل الواردة

**As** a teacher,
**I want** to list enrollment requests for my courses,
**so that** I approve or reject them.

**Endpoints:**
- `GET /Api/V1/Teacher/EnrollmentRequests` → `GetCourseEnrollmentRequestsQueryHandler`

**Status:** `implemented`

---

### S1-TE-007: عرض تفاصيل طلب تسجيل

**As** a teacher,
**I want** to view one enrollment request,
**so that** I see proposed slots and group members.

**Endpoints:**
- `GET /Api/V1/Teacher/EnrollmentRequests/{id}` → `GetTeacherEnrollmentRequestByIdQueryHandler`

**Status:** `implemented`

---

### S1-TE-008: الموافقة على طلب تسجيل

**As** a teacher,
**I want** to approve a flexible-course enrollment request,
**so that** the student can pay.

**Source:** `[code]` `ApproveEnrollmentRequestCommandHandler` → `EnrollmentApprovalService`

**Endpoints:**
- `POST /Api/V1/Teacher/EnrollmentRequests/{id}/Approve` → `ApproveEnrollmentRequestCommandHandler`

**Entities touched:** `CourseEnrollmentRequest`, `Enrollment`

**Status:** `implemented`

---

### S1-TE-009: رفض طلب تسجيل

**As** a teacher,
**I want** to reject an enrollment request with reason,
**so that** the student is informed.

**Endpoints:**
- `POST /Api/V1/Teacher/EnrollmentRequests/{id}/Reject` → `RejectEnrollmentRequestCommandHandler`

**Status:** `implemented`

---

### S1-TE-010: عرض المسجلين في دورة

**As** a teacher,
**I want** to list enrollments for a course,
**so that** I see individual and group enrollments.

**Endpoints:**
- `GET /Api/V1/Teacher/Courses/{courseId}/Enrollments` → `GetCourseEnrollmentsListQueryHandler`

**Status:** `implemented`

---

### S1-TE-011: عرض تفاصيل تسجيل

**As** a teacher,
**I want** to view enrollment detail with participants and schedules,
**so that** I prepare for sessions.

**Endpoints:**
- `GET /Api/V1/Teacher/Enrollments/{id}` → `GetTeacherEnrollmentByIdQueryHandler`

**Status:** `implemented`

---

### S1-TE-012: إدارة توفر المعلم

**As** a teacher,
**I want** to save my weekly availability and exceptions,
**so that** students pick valid slots.

**Source:** `[code]` `TeacherAvailabilityController`, `SaveTeacherAvailabilityCommandHandler`

**Endpoints:**
- `GET /Api/V1/Teacher/TeacherAvailability` → availability query handlers
- `POST /Api/V1/Teacher/TeacherAvailability` → `SaveTeacherAvailabilityCommandHandler`
- `POST /Api/V1/Teacher/TeacherAvailability/exceptions` → exception handlers
- `DELETE /Api/V1/Teacher/TeacherAvailability/exceptions/{id}` → delete exception

**Status:** `implemented`

**Notes:** Supports S1 enrollment slot picking; also used by S2 matching (planned).

---

### S1-TE-013: تسجيل الحضور

**As** a teacher,
**I want** to mark student attendance per session,
**so that** completion is tracked.

**Status:** `planned`

---

### S1-TE-014: رفع مواد الجلسة

**As** a teacher,
**I want** to upload session materials,
**so that** students access content.

**Status:** `planned`

---

### S1-TE-015: استلام مستحقات

**As** a teacher,
**I want** to view payouts for completed enrollments,
**so that** I reconcile earnings.

**Status:** `planned`

---

### 4.3 Admin stories (S1-AD-xxx)

### S1-AD-001: مراجعة والموافقة على دورات جديدة

**As** an admin,
**I want** to review and approve teacher courses before publication,
**so that** quality is controlled.

**Source:** `[planned]` No admin course controller in codebase.

**Status:** `planned`

---

### S1-AD-002: مراقبة التسجيلات

**As** an admin,
**I want** a dashboard of enrollments across courses,
**so that** I monitor platform activity.

**Status:** `planned`

---

### S1-AD-003: معالجة الاسترداد

**As** an admin,
**I want** to issue refunds for enrollments,
**so that** disputes are resolved.

**Status:** `planned`

---

### S1-AD-004: تقارير مالية للدورات

**As** an admin,
**I want** revenue reports for Scenario 1,
**so that** I track business performance.

**Status:** `planned`

---

### S1-AD-005: تعليق دورة أو معلم

**As** an admin,
**I want** to suspend a course or block a teacher,
**so that** policy violations are enforced.

**Source:** `[partial]` `TeacherManagementController` → `POST .../Block` blocks **teacher**, not course.

**Endpoints:**
- `POST /Api/V1/Admin/TeacherManagement/{teacherId}/Block` → teacher block handler

**Status:** `partially implemented`

---

## 5. Scenario 2 — Open Session Request

### 5.1 Student stories (S2-ST-xxx)

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

### 5.2 Teacher stories (S2-TE-xxx)

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

### 5.3 Admin stories (S2-AD-xxx)

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

## 6. Shared cross-cutting stories (X-xxx)

### X-001: قراءة إعدادات المصادقة (عام)

**As** a frontend client,
**I want** public auth configuration (OTP methods, registration flags),
**so that** I render the correct login UI.

**Endpoints:**
- `GET /Api/V1/Authentication/Config` → `GetAuthConfigQueryHandler`

**Permissions:** `[AllowAnonymous]`

**Status:** `implemented`

---

### X-002: تسجيل / دخول المعلم (OTP)

**As** a teacher,
**I want** to login or register via email OTP,
**so that** I access teacher features.

**Endpoints:**
- `POST /Api/V1/Authentication/Teacher/LoginOrRegister` → `SendPhoneOtpCommandHandler`
- `POST /Api/V1/Authentication/Teacher/VerifyOtp` → OTP verify handler

**Notifications:** Bilingual HTML email via queued messaging

**Status:** `implemented`

---

### X-003: تسجيل الطالب (OTP)

**As** a student,
**I want** to register via OTP and complete profile,
**so that** I enroll in courses or post session requests.

**Endpoints:**
- `POST /Api/V1/Authentication/Student/SendOtp`
- `POST /Api/V1/Authentication/Student/VerifyOtp`
- `POST /Api/V1/Authentication/Student/SetAccountTypeAndUsage`
- `POST /Api/V1/Authentication/Student/CompleteProfile`

**Status:** `implemented`

---

### X-004: تجديد JWT

**As** an authenticated user,
**I want** to refresh my access token,
**so that** my session continues securely.

**Endpoints:**
- `POST /Api/V1/Authentication/RefreshToken`

**Status:** `implemented`

---

### X-005: دخول المشرف

**As** an admin,
**I want** to login with email and password,
**so that** I access admin APIs.

**Endpoints:**
- `POST /Api/V1/Authentication/Admin/Login`

**Status:** `implemented`

---

### X-006: انتهاء مهلة الدفع التلقائي

**As** the platform,
**I want** unpaid enrollments to cancel after deadline,
**so that** capacity is freed.

**Source:** `[code]` `EnrollmentExpirationService` (`BackgroundService`)

**Entities touched:** `Enrollment` → `Cancelled`

**Status:** `implemented`

---

### X-007: رفع ملفات إلى OSS (مرفقات S2)

**As** the platform,
**I want** attachment uploads processed asynchronously to object storage,
**so that** API responds quickly.

**Source:** `[code]` `OpenSessionRequestAttachmentConsumer` in `Qalam.MessagingApi`

**Status:** `partially implemented`

---

### X-008: مراجعة وثائق المعلم (Admin)

**As** an admin,
**I want** to approve or reject teacher KYC documents,
**so that** only verified teachers operate.

**Endpoints:**
- `GET /Api/V1/Admin/TeacherManagement/Pending`
- `POST /Api/V1/Admin/TeacherManagement/{teacherId}/Documents/{documentId}/Approve`
- `POST /Api/V1/Admin/TeacherManagement/{teacherId}/Documents/{documentId}/Reject`

**Status:** `implemented`

---

## 7. Code references — quick index

| Story ID | Primary code file(s) |
|----------|---------------------|
| S1-ST-001 | `Qalam.Core/Features/Student/CourseCatalog/Queries/GetPublishedCoursesList/GetPublishedCoursesListQueryHandler.cs` |
| S1-ST-004 | `Qalam.Core/Features/Student/EnrollmentRequests/Commands/RequestCourseEnrollment/RequestCourseEnrollmentCommandHandler.cs` |
| S1-ST-010 | `Qalam.Core/Features/Student/Payments/Commands/PayEnrollmentParticipant/PayEnrollmentParticipantCommandHandler.cs` |
| S1-TE-003 | `Qalam.Core/Features/Teacher/CourseManagement/Commands/CreateCourse/CreateCourseCommandHandler.cs` |
| S1-TE-008 | `Qalam.Core/Features/Teacher/EnrollmentRequests/Commands/ApproveEnrollmentRequest/ApproveEnrollmentRequestCommandHandler.cs` |
| S2-ST-001 | `Qalam.Core/Features/Student/OpenSessionRequests/Commands/CreateOpenSessionRequest/CreateOpenSessionRequestCommandHandler.cs` |
| S2-ST-005 | `Qalam.Core/Features/Student/OpenSessionRequests/Commands/UploadOpenSessionRequestAttachment/UploadOpenSessionRequestAttachmentCommandHandler.cs` |
| X-001 | `Qalam.Core/Features/Authentication/Queries/GetAuthConfig/GetAuthConfigQueryHandler.cs` |
| X-006 | `Qalam.Service/BackgroundServices/EnrollmentExpirationService.cs` |
| S2-TE-* | `docs/TEACHER-ROLE-Scenario2.md` (spec only) |
| S2-AD-* | `docs/ADMIN-ROLE-Scenario2.md` (spec only) |

---

## 8. Endpoint inventory

| METHOD | PATH | HANDLER | ROLE | STORY ID(S) |
|--------|------|---------|------|-------------|
| GET | `/Api/V1/Student/Courses` | `GetPublishedCoursesListQueryHandler` | Student, Guardian | S1-ST-001 |
| GET | `/Api/V1/Student/Courses/Recommended` | `GetRecommendedCoursesQueryHandler` | Student, Guardian | S1-ST-002 |
| GET | `/Api/V1/Student/Courses/{id}` | `GetPublishedCourseByIdQueryHandler` | Student, Guardian | S1-ST-003 |
| POST | `/Api/V1/Student/EnrollmentRequests` | `RequestCourseEnrollmentCommandHandler` | Student, Guardian | S1-ST-004, S1-ST-005 |
| GET | `/Api/V1/Student/EnrollmentRequests` | `GetMyEnrollmentRequestsQueryHandler` | Student, Guardian | S1-ST-007 |
| GET | `/Api/V1/Student/EnrollmentRequests/{id}` | `GetMyEnrollmentRequestByIdQueryHandler` | Student, Guardian | S1-ST-008 |
| POST | `/Api/V1/Student/EnrollmentRequests/{enrollmentRequestId}/Members/Response` | `RespondToGroupEnrollmentInviteCommandHandler` | Student, Guardian | S1-ST-006 |
| GET | `/Api/V1/Student/Teachers/{teacherId}/Availability` | `GetTeacherAvailabilityByRangeQueryHandler` | Student, Guardian | S1-ST-009 |
| POST | `/Api/V1/Student/Payments/Participants` | `PayEnrollmentParticipantCommandHandler` | Student, Guardian | S1-ST-010 |
| GET | `/Api/V1/Student/Payments/Enrollments/{enrollmentId}/Summary` | `GetEnrollmentPaymentSummaryQueryHandler` | Student, Guardian | S1-ST-011 |
| GET | `/Api/V1/Student/Enrollments` | `GetMyEnrollmentsQueryHandler` | Student, Guardian | S1-ST-012 |
| GET | `/Api/V1/Student/Enrollments/{id}` | `GetMyEnrollmentByIdQueryHandler` | Student, Guardian | S1-ST-013 |
| GET | `/Api/V1/Student/Invitations` | `GetMyInvitationsQueryHandler` | Student, Guardian | S1-ST-014 |
| GET | `/Api/V1/Student/Students/Search` | `SearchStudentsForGroupQueryHandler` | Student, Guardian | S1-ST-015 |
| GET | `/Api/V1/Teacher/TeacherCourse` | `GetCoursesListQueryHandler` | Teacher | S1-TE-001 |
| GET | `/Api/V1/Teacher/TeacherCourse/{id}` | `GetCourseByIdQueryHandler` | Teacher | S1-TE-002 |
| POST | `/Api/V1/Teacher/TeacherCourse` | `CreateCourseCommandHandler` | Teacher | S1-TE-003 |
| PUT | `/Api/V1/Teacher/TeacherCourse/{id}` | `UpdateCourseCommandHandler` | Teacher | S1-TE-004 |
| DELETE | `/Api/V1/Teacher/TeacherCourse/{id}` | `DeleteCourseCommandHandler` | Teacher | S1-TE-005 |
| GET | `/Api/V1/Teacher/EnrollmentRequests` | `GetCourseEnrollmentRequestsQueryHandler` | Teacher | S1-TE-006 |
| GET | `/Api/V1/Teacher/EnrollmentRequests/{id}` | `GetTeacherEnrollmentRequestByIdQueryHandler` | Teacher | S1-TE-007 |
| POST | `/Api/V1/Teacher/EnrollmentRequests/{id}/Approve` | `ApproveEnrollmentRequestCommandHandler` | Teacher | S1-TE-008 |
| POST | `/Api/V1/Teacher/EnrollmentRequests/{id}/Reject` | `RejectEnrollmentRequestCommandHandler` | Teacher | S1-TE-009 |
| GET | `/Api/V1/Teacher/Courses/{courseId}/Enrollments` | `GetCourseEnrollmentsListQueryHandler` | Teacher | S1-TE-010 |
| GET | `/Api/V1/Teacher/Enrollments/{id}` | `GetTeacherEnrollmentByIdQueryHandler` | Teacher | S1-TE-011 |
| POST | `/Api/V1/Student/OpenSessionRequests` | `CreateOpenSessionRequestCommandHandler` | Student, Guardian | S2-ST-001 |
| GET | `/Api/V1/Student/OpenSessionRequests/my` | `GetMyOpenSessionRequestsQueryHandler` | Student, Guardian | S2-ST-002 |
| GET | `/Api/V1/Student/OpenSessionRequests/{id}` | `GetOpenSessionRequestByIdQueryHandler` | Student, Guardian | S2-ST-003 |
| POST | `/Api/V1/Student/OpenSessionRequests/{id}/Cancel` | `CancelOpenSessionRequestCommandHandler` | Student, Guardian | S2-ST-004 |
| POST | `/Api/V1/Student/OpenSessionRequests/{id}/Attachments` | `UploadOpenSessionRequestAttachmentCommandHandler` | Student, Guardian | S2-ST-005 |
| DELETE | `/Api/V1/Student/OpenSessionRequests/{id}/Attachments/{attachmentId}` | `DeleteOpenSessionRequestAttachmentCommandHandler` | Student, Guardian | S2-ST-006 |
| POST | `/Api/V1/Student/OpenSessionRequests/{openSessionRequestId}/Members/Response` | `RespondToOpenSessionRequestInvitationCommandHandler` | Student, Guardian | S2-ST-007 |
| GET | `/Api/V1/Authentication/Config` | `GetAuthConfigQueryHandler` | Anonymous | X-001 |

_Planned endpoints (S2 Teacher/Admin, Conversations) are documented in role MD files — not duplicated here until implemented._

---

## 9. Entity inventory

| ENTITY | NAMESPACE | RELATED STORY IDS |
|--------|-----------|-------------------|
| `Course` | `Qalam.Data.Entity.Course` | S1-ST-001–003, S1-TE-001–005 |
| `CourseSession` | `Qalam.Data.Entity.Course` | S1-ST-003, S1-TE-003 |
| `CourseEnrollmentRequest` | `Qalam.Data.Entity.Course` | S1-ST-004–008, S1-TE-006–009 |
| `CourseRequestGroupMember` | `Qalam.Data.Entity.Course` | S1-ST-005–006 |
| `Enrollment` | `Qalam.Data.Entity.Course` | S1-ST-010–013, S2-ST-011 (planned) |
| `EnrollmentParticipant` | `Qalam.Data.Entity.Course` | S1-ST-010–011 |
| `CourseSchedule` | `Qalam.Data.Entity.Course` | S1-ST-013, S1-TE-011 |
| `Payment` | `Qalam.Data.Entity.Payment` | S1-ST-010–011 |
| `EnrollmentPayment` | `Qalam.Data.Entity.Payment` | S1-ST-010 |
| `TeacherAvailability` | `Qalam.Data.Entity.Teacher` | S1-ST-009, S1-TE-012 |
| `TeacherReview` | `Qalam.Data.Entity.Teacher` | S1-ST-001, S1-ST-017 |
| `OpenSessionRequest` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-001–004 |
| `OpenSessionOffer` | `Qalam.Data.Entity.OpenSessionRequests` | S2-TE-005–008, S2-ST-009–010 |
| `OpenSessionRequestInvitation` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-007 |
| `OpenSessionRequestAttachment` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-005–006 |
| `OpenSessionRequestTarget` | `Qalam.Data.Entity.OpenSessionRequests` | S2-TE-002 (planned) |
| `OfferConversation` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-012, S2-TE-009 |
| `OfferMessage` | `Qalam.Data.Entity.OpenSessionRequests` | S2-ST-012, S2-TE-009 |
| `SessionRequest` (legacy) | `Qalam.Data.Entity.Session` | — (do not use for S2) |

---

## 10. Event inventory

| EVENT NAME | TYPE | PUBLISHER | SUBSCRIBERS | STORY IDS |
|------------|------|-----------|-------------|-----------|
| _(none — no domain event bus)_ | — | — | — | — |
| `SessionRequestPublished` | Integration (planned) | Matching engine (planned) | Notification service (planned) | S2-TE-001 |
| SignalR `OfferMessageReceived` | SignalR (planned) | Chat hub (planned) | Teacher/Student clients | S2-ST-012, S2-TE-009 |
| Enrollment payment completed | Implicit (handler) | `PayEnrollmentParticipantCommandHandler` | `ScheduleGenerationService` | S1-ST-010 |
| Enrollment expired | Background job | `EnrollmentExpirationService` | — | S1-ST-018, X-006 |
| Email OTP queued | Integration | `OtpService` | `EmailConsumerService` | X-002, X-003 |
| Attachment upload queued | Integration | Upload handler | `OpenSessionRequestAttachmentConsumer` | S2-ST-005, X-007 |

---

## 11. Discrepancies between BRD and code

| AREA | BRD / DOC SAYS | CODE SAYS | RESOLUTION |
|------|----------------|-----------|------------|
| Entity naming | `SessionRequest`, `SessionOffer` | `OpenSessionRequest`, `OpenSessionOffer` (`sr` schema) | **Code is source**; docs use domain aliases |
| Request IDs | GUID + `requestNumber` (e.g. SR-2026-0145) | `int` identity only | **TBD** — add display number or adopt GUID |
| Matching on create | Controller XML: matching runs when published | Handler: `P3` TODO, no targets written | **Code is source** — matching not live |
| Draft wizard | Multi-step save draft | Single `POST` creates published request; `Draft` enum unused | **Code is source** until draft API added |
| `ReceivingOffers` status | Active marketplace phase | No handler transitions into this status | **Planned** |
| Teacher offer schedule | Teacher proposes dates in BRD | `OpenSessionOffer` comment: student times implicit | **TBD** with product |
| Legacy `session.SessionRequest` | — | Separate table from Scenario 2 | **Do not conflate**; migrate or remove legacy |
| Admin course approval | Required in S1 checklist | No admin course endpoints | **Planned** |
| Zoom / live session | Join link in session flow | `canStart` flag only, no join API | **Planned** |
| Payment gateway | Real payments | Mock always succeeds | **Code is source** for current env |
| SignalR chat | Full conversation API | No hub, no controller | **Planned** per role docs |

---

## 12. Open questions

1. **Legacy `SessionRequest` (`session` schema):** Should it be deleted, migrated to `sr.OpenSessionRequest`, or kept for another feature?
2. **Matching engine (P3):** What triggers `OpenSessionRequestTarget` creation — synchronous on publish, background job, or admin rules first?
3. **Unified `Enrollment` for S2:** After offer acceptance, is enrollment created with `EnrollmentSource.OpenSessionRequest` using the same payment flow as S1?
4. **Request display ID:** Will the API expose a human-readable `requestNumber` while keeping `int` PK internally?
5. **Course session CRUD:** Should outline sessions be managed via nested DTO on `UpdateCourse` only, or dedicated `/TeacherCourse/{id}/Sessions` routes?
6. **Admin S1 scope:** Is course pre-approval required before `CourseStatus.Published`, or is teacher self-publish the long-term model?
7. **Real payment provider:** Which gateway (Moyasar, HyperPay, etc.) and webhook shape for both scenarios?

---

## Summary statistics

| Metric | Value |
|--------|-------|
| **Total stories** | **72** |
| S1 Student | 18 (14 implemented, 1 partial, 3 planned) |
| S1 Teacher | 15 (12 implemented, 3 planned) |
| S1 Admin | 5 (1 partial, 4 planned) |
| S2 Student | 12 (7 implemented, 1 partial, 4 planned) |
| S2 Teacher | 11 (all planned) |
| S2 Admin | 8 (all planned) |
| Cross-cutting | 8 (6 implemented, 2 partial) |
| **Implemented** | **~39 (54%)** |
| **Partially implemented** | **~4 (6%)** |
| **Planned** | **~29 (40%)** |
| **Discrepancies (§11)** | **11** |
| **Open questions (§12)** | **7** |

### Top 3 risks

1. **S2 matching deferred (P3)** while student UI/docs imply teachers receive requests immediately after publish.
2. **Dual session-request models** (`session.SessionRequest` vs `sr.OpenSessionRequest`) causing migration and developer confusion.
3. **No admin or financial APIs** for either scenario in production ops — refunds, disputes, and reports exist only on paper.

---

_Generated from `docs/PROMPT-Consolidated-User-Stories.md` investigation against Qalam codebase, 2026-06-03._

```json

```