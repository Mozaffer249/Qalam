# Teacher registration flow (v2)

End-to-end onboarding for new teachers: auth → documents → **subjects (before activation)** → admin review → **Active** → availability.

> **Full cycle (single page):** [Teacher-Registration-Cycle.md](Teacher-Registration-Cycle.md)  
> **Full API reference:** [Teacher-Registration-Guide.md](Teacher-Registration-Guide.md)  
> **Subject picker wizard:** [Education_Business_Logic.md](../Qalam.Data/AppMetaData/docs/Education_Business_Logic.md) + [Teacher-Availability-and-Subjects.md](Teacher-Availability-and-Subjects.md)  
> **Admin subject review UI:** [Admin-Teacher-Subjects-Frontend.md](Admin-Teacher-Subjects-Frontend.md)  
> **Teacher app screens:** [TEACHER_PROFILE_SETTINGS_GUIDE.md](../TEACHER_PROFILE_SETTINGS_GUIDE.md)

---

## What changed (v1 → v2)

| Topic | v1 | v2 (current) |
|-------|----|--------------|
| When teacher adds subjects | After admin sets account **Active** | While **`PendingVerification`** (also allowed in `AwaitingDocuments`, `DocumentsRejected`, `Active`) |
| New `TeacherSubject` status | Auto-**Approved** | **Approved** when saved (domain requirements must be approved first) |
| Account activation | All required documents approved | Docs + domain Q approved per domain + ≥1 subject → admin **POST Activate** |
| Admin subject action | Inactivate / Reject / Restore only | **Inactivate / Activate / Restore** only (no per-subject approve/reject during registration) |
| Domain verification | N/A | Admin reviews **domain questions** per education domain; subject save blocked until domain approved |
| Active teacher adds subject | Immediately usable | Row **Approved** if domain already approved; cascade-rejected rows fixed via domain re-approval |

---

## Summary

| Phase | Who | What |
|-------|-----|------|
| 1 — Auth | Teacher | OTP login/register, name + password |
| 2 — Requirements | Teacher | Upload identity + certificates, bio, location (dynamic catalog) |
| 3 — Domain questions | Teacher | Answer required questions for **every catalog domain** with required Q (`GET /Teacher/DomainQuestions/status` → `POST /Teacher/DomainQuestions/submit`) |
| 3b — Domain review | Admin | Approve/reject domain question submissions |
| 4 — Subjects | Teacher | Pick catalog subjects/units **after all catalog domains approved** |
| 5 — Review | Admin | Approve/reject **documents**; activate when `canBeActivated` |
| 6 — Active | Admin | `POST /TeacherManagement/{teacherId}/Activate` when `canBeActivated` is true |
| 7 — Availability | Teacher | Weekly schedule (after Active only) |

**Key flow:** after document upload, `nextStep` is **Complete Domain Questions** (not Add Subjects). Teachers wait for **Awaiting Domain Verification** until admin approves all catalog domains, then **Add Teaching Subjects and Units**. Login always succeeds during domain pending/reject (only `Blocked` is denied); `nextStep` routes the app.

---

## Flow diagram

```mermaid
flowchart TB
  subgraph teacher [Teacher app]
    S0[0 Auth config]
    S1[1-2 OTP]
    S3[3 Personal info]
    S4[4 Load requirements]
    S5[5 Submit requirements]
    S5a[5a Domain questions]
    S5b[5b Wait domain review]
    S5c[5c Add subjects]
    S6[6 Wait for review]
    S7[7 Set availability]
    S0 --> S1 --> S3 --> S4 --> S5
    S5 --> S5a --> S5b --> S5c
    S5c --> S6
    S6 --> S7
  end

  subgraph admin [Admin panel]
    Q[Pending queue]
    R[Review docs + domain questions]
    Q --> R
  end

  S5 -->|PendingVerification| Q
  S5a -->|domain Q submitted| Q
  S5b -->|awaiting domain approval| Q
  S5c -->|subjects saved| Q
  R -->|docs + domain Q approved + ≥1 subject| AUTH[Admin POST Activate]
  AUTH --> ACTIVE[Teacher Active]
  ACTIVE --> S7
```

---

## Teacher status machine

```mermaid
stateDiagram-v2
  [*] --> AwaitingDocuments : Step 3 CompletePersonalInfo
  AwaitingDocuments --> PendingVerification : SubmitRegistrationRequirements
  PendingVerification --> PendingVerification : POST TeacherSubject (Approved when domain OK)
  PendingVerification --> PendingVerification : Admin partial doc/domain approve
  PendingVerification --> DocumentsRejected : Reject required document or domain question
  DocumentsRejected --> PendingVerification : Teacher re-upload / fix domain Q
  PendingVerification --> Active : Admin POST Activate when canBeActivated
  Active --> Active : Teacher adds subject (Approved if domain OK)
  PendingVerification --> Blocked : Admin block
  Active --> Blocked : Admin block
```

| `TeacherStatus` | Meaning |
|-----------------|--------|
| `AwaitingDocuments` | Profile created; must submit requirements |
| `PendingVerification` | Requirements submitted; may still need subjects or admin review |
| `DocumentsRejected` | A required document was rejected; re-upload needed |
| `Active` | Fully approved; can set availability and create courses |
| `Blocked` | Admin blocked account | No authenticated API access (global middleware) |

---

## Step-by-step (teacher)

### Steps 0–3 — Auth & profile

| Step | API | Notes |
|------|-----|-------|
| 0 | `GET /Api/V1/Authentication/Config` | Phone/email/OTP UI rules |
| 1 | `POST /Api/V1/Authentication/Teacher/LoginOrRegister` | Send OTP |
| 2 | `POST /Api/V1/Authentication/Teacher/VerifyOtp` | Returns JWT; `nextStep` in response |
| 3 | `POST /Api/V1/Authentication/Teacher/CompletePersonalInfo` | Creates teacher → `AwaitingDocuments` |

Use `nextStep` from VerifyOtp / CompletePersonalInfo (via `GetNextRegistrationStepAsync`) to drive the wizard.

### Step 4 — Load requirements

```http
GET /Api/V1/Authentication/Teacher/RegistrationRequirements
```

Build dynamic form from `requirements[]` (file, text, boolean, selection fields).

### Step 5 — Submit requirements

```http
POST /Api/V1/Authentication/Teacher/SubmitRegistrationRequirements
Authorization: Bearer <teacher-jwt>
Content-Type: multipart/form-data
```

Sets `teacher.Status = PendingVerification`. Files queue to OSS via RabbitMQ → MessagingApi.

**Response** includes `nextStep` (usually → **Complete Domain Questions** when catalog domains have required questions):

```json
{
  "succeeded": true,
  "data": {
    "message": "Registration submitted successfully.",
    "nextStep": {
      "currentStep": 4,
      "nextStep": 5,
      "nextStepName": "Complete Domain Questions",
      "isRegistrationComplete": false,
      "message": "Complete the required domain verification questions for each education domain before adding teaching subjects."
    }
  }
}
```

Track progress:

```http
GET /Api/V1/Teacher/TeacherDocuments/Status
GET /Api/V1/Authentication/Teacher/AccountStatus
```

### Step 5a — Complete domain questions (before subjects)

**When:** `nextStepName === "Complete Domain Questions"` or **Fix Domain Verification** (after admin reject).

1. `GET /Api/V1/Teacher/DomainQuestions/status` — per-domain checklist
2. For each catalog domain with required questions: `POST /Api/V1/Teacher/DomainQuestions/submit`
3. Response includes optional `nextStep` for immediate navigation

**When all catalog domains submitted:** `nextStepName` → **Awaiting Domain Verification** (poll `AccountStatus`).

**When admin rejects:** login still works → **Fix Domain Verification** with `pendingCorrections[]`; resubmit rejected answers.

### Step 5b — Add teaching subjects (after all catalog domains approved)

**When:** `nextStepName === "Add Teaching Subjects and Units"` (all catalog domain Q approved; no subject offerings yet).  
**Also allowed:** `AwaitingDocuments`, `DocumentsRejected`, and `Active` (blocked only: `Blocked`).

**Domain questions (first time per domain):** Before the filter wizard for a domain, load domains with embedded questions. See [Teacher-Domain-Questions.md](Teacher-Domain-Questions.md).

1. `GET /Api/V1/Education/Domains` — if the chosen domain has `requiresAnswer: true`, show the questionnaire from `questions[]` and submit via `POST /Api/V1/Teacher/DomainQuestions/submit` with `domainId` and `answers[]` (multipart; see [Teacher-Domain-Questions.md](Teacher-Domain-Questions.md)).
2. Run the education wizard — `GET /Api/V1/Education/filter-options` (see [Education_Business_Logic.md](../Qalam.Data/AppMetaData/docs/Education_Business_Logic.md)).
3. Save selection:

```http
POST /Api/V1/Teacher/TeacherSubject
Authorization: Bearer <teacher-jwt>
Content-Type: application/json

{
  "subjects": [
    {
      "subjectId": 12,
      "canTeachFullSubject": false,
      "units": [{ "unitId": 44, "quranContentTypeId": null, "quranLevelId": null }]
    }
  ]
}
```

Each new row: `verificationStatus = Pending`, `isActive = true`. Teacher sees **Pending review** pill.

**Response** (`POST` success, newly added subject):

```json
{
  "succeeded": true,
  "data": {
    "teacherId": 12,
    "subjects": [
      {
        "id": 101,
        "subjectId": 12,
        "subjectNameAr": "الرياضيات",
        "subjectNameEn": "Mathematics",
        "domainCode": "school",
        "canTeachFullSubject": false,
        "isActive": true,
        "verificationStatus": 1,
        "rejectionReason": null,
        "reviewedAt": null,
        "units": [{ "id": 1, "unitId": 44, "unitNameAr": "...", "unitNameEn": "..." }]
      }
    ],
    "nextStep": {
      "currentStep": 5,
      "nextStep": 0,
      "nextStepName": "Awaiting Admin Verification",
      "isRegistrationComplete": false
    }
  }
}
```

List offerings:

```http
GET /Api/V1/Teacher/TeacherSubject
```

### Step 6 — Awaiting admin verification

When at least one subject exists and docs are submitted, `nextStepName === "Awaiting Admin Verification"`.

Teacher waits; admin approves documents and domain question submissions. When `canBeActivated === true` on `GET /Admin/TeacherManagement/{teacherId}`, admin authorizes the account:

```http
POST /Api/V1/Admin/TeacherManagement/{teacherId}/Activate
```

Prerequisites:

- Every **active required** registration submission is `Approved`, **and**
- All **required domain questions** are `Approved` for each domain the teacher has subjects in, **and**
- `subjectSummary.totalSubjects >= 1`.

### Step 7 — Availability (after Active)

```http
GET /Api/V1/Teacher/TeacherAvailability?fromDate=...&toDate=...
POST /Api/V1/Teacher/TeacherAvailability
```

Requires `TeacherStatus.Active`.

---

## Teacher lifecycle emails

Queued bilingual emails (EN/AR) via RabbitMQ → Messaging API. Login links use `PlatformSettings.WebAppBaseUrl` (env: `PLATFORM_WEB_APP_BASE_URL`, default `https://qalam.net.sa/`).

| Event | When | Login CTA |
|-------|------|-----------|
| Registration received | First submit (`AwaitingDocuments` → `PendingVerification`) | Yes |
| Document rejected | Admin rejects a document | Yes |
| Domain question rejected | Admin rejects a domain submission | Yes (login still works — route to fix screen) |
| Account activated | Admin `POST .../Activate` succeeds | Yes |
| Account blocked | Admin toggles Block on | No (support copy only) |
| Account unblocked | Admin toggles Block off (same endpoint) | Yes |

**Not emailed:** per-document/subject approve, “ready for activation” nudge, teacher re-upload back to pending.

Email failures are logged only — they never roll back the underlying status change.

---

## Blocked teacher access control

When `Teacher.Status = Blocked`, **every authenticated API request** for that user is rejected by [`BlockedTeacherMiddleware`](../Qalam.Core/MiddleWare/BlockedTeacherMiddleware.cs) with **403 Forbidden** and the localized `AccountBlocked` message — courses, availability, documents, registration, refresh token, etc.

Unauthenticated entrypoints (`LoginOrRegister`, `VerifyOtp`) still rely on handler checks so blocked users cannot obtain a new session. Existing JWTs remain valid until expiry but cannot call any authenticated endpoint.

---

## Admin review

### Queue

```http
GET /Api/V1/Admin/TeacherManagement/Pending?pageNumber=1&pageSize=10
```

Teachers in `PendingVerification` or `DocumentsRejected`.

### Teacher detail

```http
GET /Api/V1/Admin/TeacherManagement/{teacherId}
```

Use:

- `documents[]` — identity + **certificates** (`documentType === 2`)
- `subjects[]` + `subjectSummary` — teaching offerings (informational; not individually approved during registration)
- `domainQuestionSubmissions[]` — per-domain verification checklist
- `canBeActivated` — `true` when docs + domain Q + ≥1 subject ready; admin must `POST .../Activate`
- `registrationRequirements[]` — dynamic catalog checklist

### Document actions

```http
POST /Api/V1/Admin/TeacherManagement/{teacherId}/Documents/{documentId}/Approve
POST /Api/V1/Admin/TeacherManagement/{teacherId}/Documents/{documentId}/Reject
Body: { "reason": "..." }
```

### Subject moderation (post-activation)

```http
POST /Api/V1/Admin/TeacherManagement/{teacherId}/Subjects/{teacherSubjectId}/Inactivate
POST /Api/V1/Admin/TeacherManagement/{teacherId}/Subjects/{teacherSubjectId}/Activate
POST /Api/V1/Admin/TeacherManagement/{teacherId}/Subjects/{teacherSubjectId}/Restore
```

During registration, admin reviews **domain question submissions** instead of approving individual subjects. See [Teacher-Domain-Questions.md](Teacher-Domain-Questions.md).

---

## Backend implementation

| Concern | Location |
|---------|----------|
| New subjects → `Approved` (when domain OK) | `TeacherSubjectRepository.AddNewSubjectsAsync` |
| Subject save blocked until domain approved | `SaveTeacherSubjectsCommandHandler` + `ITeacherDomainSubjectCascadeService` |
| Domain approve → auto-approve subjects in domain | `TeacherDomainSubjectCascadeService.ApproveSubjectsInDomainAsync` |
| Pre-activation POST allowed | `SaveTeacherSubjectsCommandHandler` (blocked users rejected by middleware) |
| Registration wizard order | `TeacherRegistrationService.GetNextRegistrationStepAsync` |
| Activation gate (docs + domain Q + ≥1 subject) | `TeacherRegistrationCompletionService.CanActivateTeacherAccountAsync` |
| Admin authorize account | `POST .../Activate` → `ActivateTeacherAccountAsync` |
| Courses / matching (unchanged) | Require `VerificationStatus.Approved` + `TeacherStatus.Active` |

**Deploy:** apply pending EF migrations if any, then `docker compose ... up -d --build`.

---

## `nextStep` guide (teacher app)

Derived from `GetNextRegistrationStepAsync` + auth responses:

| `teacher.Status` | Condition | `nextStepName` | UI |
|------------------|-----------|----------------|-----|
| *(no profile)* | — | Complete Personal Information | Step 3 |
| `AwaitingDocuments` | — | Upload Documents | Step 5 form |
| `PendingVerification` | rejected domain question(s) | **Fix Domain Verification** | domain-questions screen (`pendingCorrections[]`) — **login allowed** |
| `PendingVerification` or `DocumentsRejected` | required domain answers missing | **Complete Domain Questions** | domain-questions screen |
| Same | all catalog domains submitted, admin review pending | **Awaiting Domain Verification** | waiting screen — poll `AccountStatus` |
| Same | all catalog domains approved, no subjects | **Add Teaching Subjects and Units** | subject wizard |
| `PendingVerification` | has subjects, review in progress | **Awaiting Admin Verification** | waiting screen |
| `PendingVerification` | has subjects, `canBeActivated` | **Awaiting Final Approval** | waiting screen |
| `DocumentsRejected` | rejected docs exist | **Re-upload Rejected Documents** | documents list |
| `DocumentsRejected` | rejected domain question(s) | **Fix Domain Verification** | domain-questions screen — **login allowed** |
| `Active` | any | **Dashboard** | teacher dashboard (`requiresAvailabilitySetup` flag on `nextStep` / Status) |

**Login (`VerifyOtp`):** active teachers always get `nextStepName = "Dashboard"` — not availability.

**Waiting page poll** (`GET /Authentication/Teacher/AccountStatus` — lightweight):

- Poll every few seconds on the waiting screen
- `awaitingFinalApproval === true` → show **Awaiting final approval** section
- `isAccountActivated` flips to `true` and `requiresAvailabilitySetup === true` → navigate to availability setup
- `isAccountActivated && !requiresAvailabilitySetup` → navigate to dashboard (`nextStep.nextStepName`)

Account status response fields: `teacherStatus`, `isAccountActivated`, `canBeActivated`, `awaitingFinalApproval`, `requiresAvailabilitySetup`, `nextStep` (includes `pendingCorrections[]` when corrections are needed).

**Full checklist** (`GET /Teacher/TeacherDocuments/Status` — use when rejection reasons or re-upload IDs are needed):

Status response fields: `teacherStatus`, `isAccountActivated`, `canBeActivated`, `awaitingFinalApproval`, `requiresAvailabilitySetup`, `subjectSummary`, `requirements`, `legacyDocuments`.

---

## Subject verification states

| `verificationStatus` | Teacher UI | Admin actions | Usable for courses? |
|---------------------|------------|---------------|---------------------|
| `Approved` (2) | Active / Inactive | Inactivate, Restore (if cascade-rejected) | Yes (if `Active` teacher + `isActive`) |
| `Rejected` (3) — cascade | Read-only — fix domain questions first | Restore after domain re-approved | No |

New subjects from `POST /Teacher/TeacherSubject` are created as **Approved** when the domain's required questions are already approved. Save returns `400` if domain questions are missing, pending, or rejected.

When admin rejects a domain question, existing subjects in that domain are cascade-rejected. After domain re-approval, all subjects in that domain are auto-approved again.

---

## Activation checklist

All must be true for `canBeActivated` (admin **Authorize account** button):

1. Teacher not already `Active` or `Blocked`
2. All **active required** `TeacherRegistrationSubmission` rows → `Approved`
3. All **required domain questions** → `Approved` for each relevant domain
4. At least **one** `TeacherSubject` row exists

Then admin calls `POST /Api/V1/Admin/TeacherManagement/{teacherId}/Activate`.

---

## Document file URLs (OSS)

Uploaded files are stored in Alibaba OSS. `filePath` in API responses is the object URL. If the bucket is private, opening the URL in a browser returns `AccessDenied` — configure bucket read policy or use presigned URLs. See [deployment/06-oss-storage.md](deployment/06-oss-storage.md).

---

## Related endpoints (quick list)

| Purpose | Method | Path |
|---------|--------|------|
| Auth config | GET | `/Api/V1/Authentication/Config` |
| OTP | POST | `/Api/V1/Authentication/Teacher/LoginOrRegister`, `.../VerifyOtp` |
| Personal info | POST | `/Api/V1/Authentication/Teacher/CompletePersonalInfo` |
| Requirements catalog | GET | `/Api/V1/Authentication/Teacher/RegistrationRequirements` |
| Submit requirements | POST | `/Api/V1/Authentication/Teacher/SubmitRegistrationRequirements` |
| Account status poll | GET | `/Api/V1/Authentication/Teacher/AccountStatus` |
| Registration checklist | GET | `/Api/V1/Teacher/TeacherDocuments/Status` |
| Re-upload document | PUT | `/Api/V1/Teacher/TeacherDocuments/{id}/Reupload` |
| Filter wizard | GET | `/Api/V1/Education/filter-options` |
| Save subjects | POST | `/Api/V1/Teacher/TeacherSubject` |
| List subjects | GET | `/Api/V1/Teacher/TeacherSubject` |
| Admin pending | GET | `/Api/V1/Admin/TeacherManagement/Pending` |
| Admin detail | GET | `/Api/V1/Admin/TeacherManagement/{teacherId}` |
| Activate account | POST | `/Api/V1/Admin/TeacherManagement/{teacherId}/Activate` |
| Inactivate subject | POST | `/Api/V1/Admin/TeacherManagement/{teacherId}/Subjects/{teacherSubjectId}/Inactivate` |
| Restore subject | POST | `/Api/V1/Admin/TeacherManagement/{teacherId}/Subjects/{teacherSubjectId}/Restore` |

---

## Out of scope (v1)

- Linking each subject to a specific certificate document ID
- Teacher delete subject (`DELETE` stubbed)
- Demoting `Active` teachers when they add a new pending subject
- Automated certificate OCR / subject matching
