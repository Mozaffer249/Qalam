# Teacher domain questions

Admin-defined questions shown when a teacher adds subjects in an education domain (school, quran, etc.). Each question is answered **once per teacher per domain**.

## Overview

| Concern | Behavior |
|---------|----------|
| Scope | Every **catalog** education domain with at least one active **required** question (e.g. school, quran, language from seed) |
| Registration order | **Before** subject selection in a domain — complete that domain's questions and get admin approval; subject wizard shows only approved domains |
| Admin review | Per-question `requiresAdminReview` flag |
| Empty catalog | No change — same flow as before |

## Teacher flow

Registration order: **documents → domain questions (per domain) → admin domain review → subjects in approved domains**.

```mermaid
sequenceDiagram
    participant App as TeacherApp
    participant API as Qalam API

    App->>API: POST SubmitRegistrationRequirements
    API-->>App: nextStep Complete Domain Questions
    App->>API: GET /Teacher/DomainQuestions/status
    App->>API: POST /Teacher/DomainQuestions/submit
    API-->>App: nextStep Awaiting Domain Verification
    Note over App,API: Admin approves all catalog domains
    API-->>App: nextStep Add Teaching Subjects
    App->>API: GET /Education/filter-options?domainId=
    App->>API: POST /Teacher/TeacherSubject
```

### 1. Load domains

```http
GET /Api/V1/Education/Domains
Authorization: Bearer <teacher-jwt>
```

For authenticated teachers, each domain includes:

```json
{
  "id": 1,
  "nameEn": "School",
  "code": "school",
  "requiresAnswer": true,
  "questions": [
    {
      "code": "school_experience_years",
      "nameEn": "Years of experience",
      "requirementType": "Text",
      "isRequired": true,
      "requiresAdminReview": false,
      "isSubmitted": false,
      "verificationStatus": null
    }
  ]
}
```

| Field | Meaning |
|-------|---------|
| `requiresAnswer` | `true` when a required question has **no** submission or a **Rejected** submission |
| `questions[]` | Active catalog for the domain |
| `isSubmitted` | Teacher already answered |
| `verificationStatus` | `null` if not submitted; else `Pending` / `Approved` / `Rejected` |
| `rejectionReason` | Populated when `verificationStatus` is `Rejected` |

Non-teacher callers receive the same shape with `requiresAnswer: false` and `questions: []`.

### Dedicated domain-questions screen

Domain questions use a **separate screen** from subject selection (`/registration/domain-questions` or `/domain-questions/:domainCode`).

```http
GET /Api/V1/Teacher/DomainQuestions/status
Authorization: Bearer <teacher-jwt>
```

Returns per-domain `requiresAnswer`, `hasRejectedAnswers`, `pendingCorrections[]`, and `questions[]` — no subject catalog mixed in.

### 2. Submit answers

```http
POST /Api/V1/Teacher/DomainQuestions/submit
Authorization: Bearer <teacher-jwt>
Content-Type: multipart/form-data

domainId=1
answers[0].code=school_experience_years
answers[0].textValue=5
answers[1].code=school_teaching_license
answers[1].files=@license.pdf
```

| Answer field | Used when `requirementType` is |
|--------------|-------------------------------|
| `code` | Always (required per item) |
| `textValue` | Text |
| `boolValue` | Boolean (`true`/`false`) |
| `selectedValues[]` | Selection (repeatable for multi-select) |
| `files[]` | File (repeatable) |

**Legacy (still supported when `answers` is omitted):** prefix fields `file_{code}`, `text_{code}`, `bool_{code}`, `select_{code}`.

Rules:

- All **required** active questions for the domain must be present in one submit (first time).
- Re-submit is allowed only when the existing submission is **Rejected** (resets to `Pending`).
- Approved or pending submissions cannot be changed (`400`).
- Unknown `code` in `answers[]` returns `400`.
- Response includes `submittedCodes[]` and optional `nextStep` for wizard navigation.
- `requiresAdminReview=false` → submission `Approved` immediately.
- `requiresAdminReview=true` → submission `Pending` until admin approves.

### Admin rejection cascade

When admin rejects a domain question that requires review:

1. The submission (and linked document, if any) is set to `Rejected`.
2. **All teacher subjects in that education domain** are auto-rejected (`rejectionSource: DomainQuestionCascade`).
3. Teacher is routed to **Fix Domain Verification** via `nextStep` / `pendingCorrections`.
4. After admin approves all required review questions in that domain, all subjects in that domain are set to **`Approved`**.

Multi-domain: rejecting school license does **not** affect quran/language subjects in other domains.

### 3. Add subjects (per-domain approval)

`POST /Api/V1/Teacher/TeacherSubject` returns `400` unless the **education domain of each subject being saved** is fully approved:

- All **required** questions must have **Approved** submissions.
- Any **submitted** answer on a question with `requiresAdminReview=true` (required or optional) must be **Approved** — **Pending** or **Rejected** blocks subject selection.
- Optional admin-review questions that were **not submitted** do not block selection.

Teachers load domains via `GET /Api/V1/Education/Domains`. The subject wizard lists **all** catalog domains; domains where `canSelectForSubjects` is false appear **disabled** with hints (`requiresAnswer`, rejected answers, or pending admin review). Optional query `forSubjectSelection=true` still filters to eligible domains only (for other callers).

Catalog domains with seeded verification questions: **school**, **quran**, **language**, **skills**, **university**.

## Admin — catalog CRUD (SuperAdmin)

Base path: `/Api/V1/Admin/TeacherDomainQuestions`

**Sample payloads & seeded defaults:** `docs/seed-data/teacher-domain-questions.json` (also inserted on startup via `TeacherDomainQuestionsSeeder` for `school`, `quran`, `language`, `skills`, `university` domains).

| Method | Path | Notes |
|--------|------|-------|
| GET | `?domainId=` | List (optional filter) |
| GET | `/{id}` | Detail |
| POST | `/` | Create (`domainId` + question payload) |
| PUT | `/{id}` | Update labels, flags, options |
| DELETE | `/{id}` | Blocked if submissions exist |
| PATCH | `/{id}/active` | Toggle `isActive` |

Question types reuse `RegistrationRequirementType`: `File`, `Text`, `Boolean`, `Selection`.

## Admin — review submissions

On teacher detail (`GET /Api/V1/Admin/TeacherManagement/{teacherId}`), `domainQuestionSubmissions` groups answers by domain.

| Method | Path |
|--------|------|
| POST | `/Api/V1/Admin/TeacherManagement/DomainQuestionSubmissions/{submissionId}/Approve` |
| POST | `/Api/V1/Admin/TeacherManagement/DomainQuestionSubmissions/{submissionId}/Reject` |

Body for reject: `{ "reason": "..." }` (same as document reject).

Only meaningful when `requiresAdminReview=true`; auto-approved answers are already `Approved`.

## Activation gate

Account activation (`canBeActivated` / `POST …/Activate`) additionally requires:

- For each domain in the teacher's **subject offerings**: all **required** questions submitted.
- For questions with `requiresAdminReview=true`: must be `Approved`.

## v1 limitations

- No re-submit after admin reject (contact support or wait for v2).
- Questions are domain-wide only (not per curriculum/level).

See also: [Teacher-Registration-Flow.md](Teacher-Registration-Flow.md), [Teacher-Registration-Guide.md](Teacher-Registration-Guide.md).
