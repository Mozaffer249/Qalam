# Enrollment Flow — Frontend Integration Guide

This is the front-end-facing guide. It walks through the **complete cycle** —
discovery → request → approval → payment → activation → schedules — with the
exact endpoints, payloads, status codes, and lifecycle transitions you need
to wire into the UI.

For deeper internals see:
- [PAYMENT_FLOW.md](PAYMENT_FLOW.md) — mock payment + schedule generation algorithm
- [DATE_RANGE_AND_AVAILABILITY.md](DATE_RANGE_AND_AVAILABILITY.md) — preferred dates, conflict detection, calendar endpoint

---

## Actors

| Actor | What they do |
|-------|--------------|
| **Student (Adult)** | Browses courses, submits requests, pays for self |
| **Guardian** | Manages minor children's enrollments, **pays on behalf of minors** |
| **Both** | A user who is both — can act in either role |
| **Invited Student** | Receives a group-enrollment invite, accepts/declines, pays their own share |
| **Teacher** | Owns the course, approves/rejects requests, sees per-date schedules |

---

## End-to-end cycle (the screens you'll wire)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Browse course catalog                                        │
│    GET /Student/Courses                                         │
│    GET /Student/Courses/{id}    ← also surfaces course.sessions │
└─────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Pick teacher availability slots + start/end dates            │
│    GET /Student/Teachers/{teacherId}/Availability               │
│         ?fromDate=...&toDate=...                                │
│    UI renders a calendar — user clicks Free slots to select     │
└─────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Submit enrollment request                                    │
│    POST /Student/EnrollmentRequests                             │
│    Body MUST include preferredStartDate + preferredEndDate      │
│    Server hard-rejects on conflicts → handle 400                │
│    On success: response includes proposedScheduleDates[]        │
│                show these dates to the user as confirmation     │
└─────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. (Group only) Invited members confirm/decline                 │
│    GET  /Student/Invitations                                    │
│    POST /Student/EnrollmentRequests/{id}/Members/Response       │
└─────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Teacher approves or rejects                                  │
│    Teacher sees proposedScheduleDates[] in detail view          │
│    Approve → CourseEnrollment / CourseGroupEnrollment created   │
│              Status = PendingPayment, deadline set (default 48h)│
└─────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Pay before deadline                                          │
│    Individual: POST /Student/Payments/Enrollment                │
│    Group:      POST /Student/Payments/GroupMember (per member)  │
│    Mock provider — always Succeeded if checks pass              │
│    On full payment: status → Active, CourseSchedules generated  │
└─────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. Active enrollment with concrete schedule                     │
│    GET /Student/Enrollments/{id}                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## Step 1 — Browse courses

| Method | Route | Notes |
|---|---|---|
| GET | `/Api/V1/Student/Courses` | Paginated catalog. Filter by `DomainId`, `SubjectId`, `TeachingModeId`, etc. |
| GET | `/Api/V1/Student/Courses/{id}` | Detail. `course.sessions[]` is empty for flexible courses. |
| GET | `/Api/V1/Student/MyChildren` | (Guardians only) list of children to enroll on behalf of. |

Use `course.teacherId` from the detail response in step 2.

---

## Step 2 — Show the teacher's calendar

**`GET /Api/V1/Student/Teachers/{teacherId}/Availability`**

Renders the date range as a calendar so the user can pick free slots.

| Query | Default | Notes |
|---|---|---|
| `fromDate` | today | Optional |
| `toDate` | `fromDate + 30 days` | Optional. Server caps at `fromDate + 90 days`. |

### Response shape

```json
{
  "data": {
    "teacherId": 7,
    "fromDate": "2026-04-28",
    "toDate":   "2026-05-28",
    "days": [
      {
        "date": "2026-05-03",
        "dayOfWeekId": 1,
        "dayNameEn": "Sunday",
        "slots": [
          { "teacherAvailabilityId": 10, "timeSlotId": 3, "startTime": "16:00:00", "endTime": "17:00:00", "durationMinutes": 60, "labelEn": "Afternoon", "status": "Free"    },
          { "teacherAvailabilityId": 12, "timeSlotId": 5, "startTime": "19:00:00", "endTime": "20:00:00", "durationMinutes": 60, "labelEn": "Evening",   "status": "Booked"  }
        ]
      }
    ]
  }
}
```

### Slot statuses (drive the UI colour)

| Status | Meaning | UX |
|---|---|---|
| `Free` | Bookable | Selectable, e.g. green |
| `Booked` | Already taken on this exact date | Disabled, grey "booked" |
| `Blocked` | Teacher's blackout (holiday, sick day) | Disabled, grey "unavailable" |

Days with no recurring availability are **omitted** entirely (don't render gaps as errors — the teacher just doesn't work that day).

---

## Step 3 — Submit the enrollment request

**`POST /Api/V1/Student/EnrollmentRequests`**

```json
{
  "data": {
    "courseId": 1,
    "studentIds": [42],
    "invitedStudentIds": [],
    "selectedAvailabilityIds": [10, 11],
    "preferredStartDate": "2026-05-15",
    "preferredEndDate":   "2026-07-15",
    "notes": "Prefers evening sessions.",
    "proposedSessions": []
  }
}
```

### Required fields (UI must collect)

- `courseId`
- `studentIds` — own students (self and/or guardian's children); at least one
- `selectedAvailabilityIds` — picked from step 2's calendar (only `Free` slots)
- **`preferredStartDate`** — must be today or later
- **`preferredEndDate`** — must be ≥ `preferredStartDate` and large enough to fit all sessions
- `proposedSessions` — required only when `course.isFlexible == true`

### Validations (server-side, surface as error toast)

| Trigger | Message format |
|---|---|
| `preferredStartDate < today` | `"PreferredStartDate must be today or later."` |
| `preferredEndDate < preferredStartDate` | `"PreferredEndDate must be on or after PreferredStartDate."` |
| Window too short for N sessions | `"Selected slots can only fit X of Y sessions before {EndDate}. Extend the end date or add another availability slot."` |
| Date already booked | `"The following dates are already booked: session 2: 2026-05-17; … . Please pick a different start date or different availability slots."` |
| Course not Published / Active | `"Course is not available for enrollment."` |
| Pending duplicate | `"You already have a pending enrollment request for this course."` |
| Individual course with > 1 student | `"Individual courses require exactly one student."` |
| Group exceeds `maxStudents` | `"Group size exceeds MaxStudents."` |

### Successful response (the **important** new fields)

```json
{
  "data": {
    "id": 123,
    "courseId": 1,
    "courseTitle": "Mathematics — Grade 10",
    "status": "Pending",
    "totalMinutes": 240,
    "estimatedTotalPrice": 200.00,
    "preferredStartDate": "2026-05-15",
    "preferredEndDate":   "2026-07-15",
    "proposedScheduleDates": [
      { "sessionNumber": 1, "date": "2026-05-17", "teacherAvailabilityId": 10, "durationMinutes": 60, "title": "Algebra Basics" },
      { "sessionNumber": 2, "date": "2026-05-19", "teacherAvailabilityId": 11, "durationMinutes": 60, "title": "Equations"      },
      { "sessionNumber": 3, "date": "2026-05-24", "teacherAvailabilityId": 10, "durationMinutes": 60, "title": "Functions"      },
      { "sessionNumber": 4, "date": "2026-05-26", "teacherAvailabilityId": 11, "durationMinutes": 60, "title": "Review"         }
    ],
    "groupMembers": [
      { "studentId": 42, "memberType": "Own", "confirmationStatus": "Confirmed" }
    ]
  },
  "succeeded": true
}
```

**Show `proposedScheduleDates` to the user immediately** as a confirmation
("These are the dates you'll attend"). They are the same dates the teacher
will see when reviewing.

---

## Step 4 — (Group only) Invitation responses

| Method | Route | Notes |
|---|---|---|
| GET | `/Api/V1/Student/Invitations` | Pending invitations for self + children |
| POST | `/Api/V1/Student/EnrollmentRequests/{id}/Members/Response` | `{ data: { studentId, decision: "Confirmed" \| "Rejected" } }` |

**Authorization rules** (the server enforces, the UI should reflect):
- **Minor student** → only the linked **guardian** can respond.
- **Adult student** → only the **student themselves** can respond.

The request stays `Pending` until either (a) all invited members respond, or (b) the teacher acts on the existing confirmed members.

---

## Step 5 — Teacher review & decision

### List & detail

| Method | Route | Notes |
|---|---|---|
| GET | `/Api/V1/Teacher/EnrollmentRequests?CourseId=X&Status=Pending` | Per-course list; status filter optional |
| GET | `/Api/V1/Teacher/EnrollmentRequests/{id}` | Detail — **includes `proposedScheduleDates[]`** so the teacher approves with concrete dates in front of them |

### Approve

`POST /Api/V1/Teacher/EnrollmentRequests/{id}/Approve`

| Course type | What's created |
|---|---|
| Individual | `CourseEnrollment` { `EnrollmentStatus = PendingPayment`, `PaymentDeadline = now + 48h` } |
| Group | `CourseGroupEnrollment` + `CourseGroupEnrollmentMember[]` (one per confirmed member, `PaymentStatus = Pending`) |

The originating request flips to `Status = Approved`.

### Reject

`POST /Api/V1/Teacher/EnrollmentRequests/{id}/Reject`

Optional body: `{ data: { rejectionReason: "..." } }` (≤ 500 chars).
Request flips to `Status = Rejected`.

---

## Step 6 — Payment (mock — always succeeds if rules pass)

After approval, the student has a payment window (default **48 hours** —
configurable on the server in `EnrollmentSettings.PaymentDeadlineHours`).

### Individual enrollment

**`POST /Api/V1/Student/Payments/Enrollment`**

```json
{ "data": { "enrollmentId": 123 } }
```

### Group enrollment (per-member)

**`POST /Api/V1/Student/Payments/GroupMember`**

```json
{ "data": { "groupEnrollmentId": 555, "studentId": 43 } }
```

`studentId` is the **member being paid for**, not the payer. The payer is
inferred from the JWT.

Each member (or their guardian) calls this endpoint **once**. The group
enrollment becomes `Active` only when the **last** member pays.

### Payer authorization (UI must enforce / hide buttons appropriately)

| Target student | Who can pay |
|---|---|
| Adult | Only the student themselves |
| Minor | Only the linked guardian |

The request owner (group leader) **cannot** pay for invited members — only the invited member's own payer can.

### Per-member share (group)

```
baseShare = round(estimatedTotalPrice / memberCount, 2)
```

The **last** payer absorbs the rounding remainder so the sum equals
`estimatedTotalPrice` to the cent. The UI doesn't compute this — the
server returns the actual `totalAmount` charged in `PaymentResultDto`.

### Successful response

```json
{
  "data": {
    "paymentId": 7001,
    "status": "Succeeded",
    "totalAmount": 200.00,
    "currency": "SAR",
    "paidAt": "2026-04-28T14:35:12Z",
    "enrollmentActivated": true,
    "schedulesCreated": 4
  }
}
```

`enrollmentActivated == true` ⇒ enrollment is now `Active`, schedules exist. Navigate the user to the enrollment detail screen.

`enrollmentActivated == false` (group, mid-flow) ⇒ keep showing "waiting for other members" UI; poll `GetGroupEnrollmentPaymentSummary` if you need progress.

### Errors to handle at pay time

| Trigger | Status | Message |
|---|---|---|
| Deadline expired | 400 | `"Payment deadline has expired."` |
| Wrong payer (not the student / not the guardian) | 400 | `"Only the [student/guardian] can pay …"` |
| Member already paid | 400 | `"This member has already paid."` |
| Race-loser (someone paid the same date+slot first) | 400 | `"Some of your scheduled dates were just booked by another student. Please re-submit with different dates."` |
| Window no longer fits | 400 | `"Schedule no longer fits before {EndDate}. Please re-submit with a longer window."` |

The mock `Payment` row is **rolled back** on race-loser / doesn't-fit, so retrying after a fresh request is safe.

### Payment summary endpoints

| Method | Route | When to call |
|---|---|---|
| GET | `/Api/V1/Student/Payments/Enrollment/{enrollmentId}/Summary` | Show "amount due / paid / deadline" on individual enrollment screen |
| GET | `/Api/V1/Student/Payments/GroupEnrollment/{groupEnrollmentId}/Summary` | Show per-member breakdown, who's paid, who's pending |

---

## Step 7 — Active enrollment (post-payment)

| Method | Route | Notes |
|---|---|---|
| GET | `/Api/V1/Student/Enrollments` | Paginated list of my enrollments |
| GET | `/Api/V1/Student/Enrollments/{id}` | Detail — includes course, teacher, status |

`CourseSchedule` rows now exist linked to the enrollment (one per session, with
the dates from `proposedScheduleDates`). UI can render them as a calendar /
session timeline.

---

## Automatic expiration (background — no FE call needed)

A background service runs every **5 minutes** and cancels enrollments whose
`PaymentDeadline` has passed:

- Individual `PendingPayment` → `Cancelled`.
- Group `PendingPayment` → `Cancelled`; **still-pending** member payments flipped to `Cancelled`. Members who already `Succeeded` stay `Succeeded` (refunds are out of scope).
- The originating request stays `Approved` (preserves the teacher's decision).

After expiry the student has to **submit a new request** — there is no "extend deadline" or "retry payment" flow.

---

## Status lifecycles (drive UI badges)

### Enrollment Request — `request.status`
```
Pending → Approved   (teacher approved)
Pending → Rejected   (teacher rejected)
```

### Enrollment — `enrollment.enrollmentStatus` / `groupEnrollment.status`
```
PendingPayment → Active     (payment(s) succeeded)
PendingPayment → Cancelled  (deadline expired or race-loser at pay time)
Active         → Completed  (all sessions done — out of scope today)
Active         → Cancelled  (manual cancellation — out of scope today)
```

### Group member payment — `member.paymentStatus`
```
Pending   → Succeeded  (member paid)
Pending   → Cancelled  (group expired before this member paid)
```

### Group invitation — `groupMember.confirmationStatus`
```
Pending → Confirmed   (accepted)
Pending → Rejected    (declined)
```

### Schedule slot status (calendar endpoint) — `slot.status`
```
Free | Booked | Blocked
```

---

## Entity relationships (mental model for the FE)

```
CourseEnrollmentRequest               ← what the student submits
├── PreferredStartDate, PreferredEndDate
├── SelectedAvailabilities[]          ← weekly slots picked from calendar
├── ProposedSessions[] (flexible)
└── GroupMembers[]                    ← Own (auto-confirmed) + Invited (Pending)

         ↓ teacher approves ↓

CourseEnrollment (individual)         CourseGroupEnrollment (group)
├── EnrollmentRequestId               ├── EnrollmentRequestId
├── PaymentDeadline                   ├── PaymentDeadline
├── EnrollmentStatus                  ├── Status
├── ActivatedAt                       ├── ActivatedAt
├── CourseEnrollmentPayments[]        └── Members[]
└── CourseSchedules[]                     ├── PaymentStatus
   (created on pay)                       ├── PaidAt
                                          ├── GroupEnrollmentMemberPayments[]
                                          └── (linked to schedules on full payment)
                                      └── CourseSchedules[]
                                         (created when ALL members paid)

CourseSchedule                        ← a concrete (Date, Slot, Duration) row
├── Date (DateOnly)
├── TeacherAvailabilityId (which weekly slot)
├── DurationMinutes
├── TeachingModeId
└── Status (Scheduled | …)
```

---

## API endpoints — full reference

### Discovery & catalog

| Method | Route | Roles |
|---|---|---|
| GET | `/Api/V1/Student/Courses` | Student, Guardian |
| GET | `/Api/V1/Student/Courses/{id}` | Student, Guardian |
| GET | `/Api/V1/Student/MyChildren` | Guardian |
| GET | `/Api/V1/Student/Teachers/{teacherId}/Availability?fromDate=&toDate=` | Student, Guardian |
| GET | `/Api/V1/Student/Students/Search?searchTerm=&maxResults=` | Student, Guardian |

### Enrollment requests (Student / Guardian)

| Method | Route | Notes |
|---|---|---|
| POST | `/Api/V1/Student/EnrollmentRequests` | New body fields: `preferredStartDate`, `preferredEndDate` |
| GET | `/Api/V1/Student/EnrollmentRequests` | Paginated, filter by `Status` |
| GET | `/Api/V1/Student/EnrollmentRequests/{id}` | Detail incl. `proposedScheduleDates[]` |
| GET | `/Api/V1/Student/Invitations` | Pending invitations for self + children |
| POST | `/Api/V1/Student/EnrollmentRequests/{id}/Members/Response` | Accept/decline group invite |

### Enrollment requests (Teacher)

| Method | Route | Notes |
|---|---|---|
| GET | `/Api/V1/Teacher/EnrollmentRequests?CourseId=X` | Per-course list |
| GET | `/Api/V1/Teacher/EnrollmentRequests/{id}` | Detail incl. `proposedScheduleDates[]` |
| POST | `/Api/V1/Teacher/EnrollmentRequests/{id}/Approve` | Creates pending-payment enrollment |
| POST | `/Api/V1/Teacher/EnrollmentRequests/{id}/Reject` | Optional `rejectionReason` |

### Payments (Student / Guardian)

| Method | Route | Notes |
|---|---|---|
| POST | `/Api/V1/Student/Payments/Enrollment` | `{ data: { enrollmentId } }` — individual |
| POST | `/Api/V1/Student/Payments/GroupMember` | `{ data: { groupEnrollmentId, studentId } }` — per-member |
| GET | `/Api/V1/Student/Payments/Enrollment/{enrollmentId}/Summary` | Amount due / paid / deadline |
| GET | `/Api/V1/Student/Payments/GroupEnrollment/{groupEnrollmentId}/Summary` | Per-member breakdown |

### My enrollments (post-payment)

| Method | Route |
|---|---|
| GET | `/Api/V1/Student/Enrollments` |
| GET | `/Api/V1/Student/Enrollments/{id}` |

---

## Frontend integration tips

1. **Always call the calendar (step 2) before the request form.** The user
   should pick from `Free` slots — submitting with `Booked` / `Blocked`
   choices will 400 at submit time.
2. **Confirm the schedule visually after submit.** Show
   `proposedScheduleDates[]` as a list/calendar so the user knows the exact
   dates they're committing to. The teacher will see the same dates.
3. **Refresh the calendar between picking and submitting** — slots can
   become `Booked` between the two screens. Treat 400 conflicts as "show the
   dates that conflicted, re-fetch the calendar, let the user re-pick."
4. **For groups, poll the group payment summary** while waiting for other
   members. `enrollmentActivated: false` from your own payment doesn't mean
   anything failed — it means others haven't paid yet.
5. **Hide pay buttons by role.** A guardian sees pay only for their minor
   children's memberships; an adult member sees pay only for their own;
   the request owner does **not** see pay for invited members.
6. **Render countdown to `paymentDeadline`.** After it passes, the
   background service cancels the enrollment within ~5 min. Surface the
   deadline prominently so users don't miss it.
7. **All datetimes are UTC.** All `DateOnly` values are calendar dates with
   no timezone — display them as-is.
8. **Currency** is server-driven (`PaymentResultDto.currency`, defaults to
   `"SAR"`). Don't hardcode the symbol.

---

## Configuration knobs (server-side, FYI)

```jsonc
// appsettings.json
{
  "EnrollmentSettings": {
    "PaymentDeadlineHours": 48,
    "ExpirationCheckIntervalMinutes": 5
  },
  "PaymentSettings": {
    "MockProviderName": "MOCK",
    "DefaultCurrency": "SAR"
  }
}
```

The FE doesn't need these — they're listed so you know which numbers to ask
the backend team to tweak when staging vs prod behaviour differs.
