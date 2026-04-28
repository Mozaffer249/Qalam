# Date Range, Conflict Detection & Teacher Availability

This document covers the second wave of changes on top of the mock-payment work:
preferred date range on enrollment requests, hard conflict detection at submit
and pay time, and a new student-facing "what is this teacher actually free on?"
calendar endpoint.

For the upstream payment + schedule generation flow see [PAYMENT_FLOW.md](PAYMENT_FLOW.md).

---

## Why this exists

Previously the schedule generator started at `today`. That meant:

1. The student had no way to say "start me in May, finish by July."
2. Conflict detection happened at payment-time — by which point money had moved
   and the only options were rolling back a paid transaction or surprising the
   user with auto-shifted dates.

The fix moves date intent to the *student* (they pick when), and shifts conflict
detection forward to *submit-time* (clean rejection, no rollback).

---

## What changed in the cycle

```
┌────────────────────────────────────────────────────────────────────────┐
│  1. SUBMIT  POST /Api/V1/Student/EnrollmentRequests                    │
│     New required body fields: PreferredStartDate, PreferredEndDate     │
│     Validator checks date sanity                                       │
│     Handler calls IScheduleGenerationService.Preview(...)              │
│     ├── if Conflicts.Count > 0   → 400  "dates already booked"         │
│     ├── if !FitsInWindow         → 400  "doesn't fit before {EndDate}" │
│     └── else                     → persist request + return            │
│                                    proposedScheduleDates[] in body     │
└────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌────────────────────────────────────────────────────────────────────────┐
│  2. TEACHER VIEW                                                       │
│     GET /Api/V1/Teacher/EnrollmentRequests/{id}                        │
│     Detail DTO now includes preferredStartDate, preferredEndDate,      │
│     and proposedScheduleDates[] (recomputed on read).                  │
│     Teacher approves with concrete dates in front of them.             │
└────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌────────────────────────────────────────────────────────────────────────┐
│  3. APPROVE  (no logic change)                                         │
└────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌────────────────────────────────────────────────────────────────────────┐
│  4. PAY                                                                │
│     effectiveStart = max(PreferredStartDate, today)                    │
│     Re-run Preview INSIDE the transaction (race-loser handling).       │
│     ├── if Conflicts → rollback, 400 "just booked by another student"  │
│     ├── if !Fits     → rollback, 400 "no longer fits"                  │
│     └── else         → write Payment + CourseSchedule rows + COMMIT    │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Algorithm details

`IScheduleGenerationService.Preview(...)` is a pure function (no I/O). Inputs:

| Input | Where from |
|---|---|
| Course | Course entity (for `IsFlexible`, `Sessions` templates, `TeachingModeId`) |
| Request | The CourseEnrollmentRequest (for `ProposedSessions` if flexible) |
| Slots | `request.SelectedAvailabilities → TeacherAvailability` (with TimeSlot, DayOfWeek) |
| Blocked exceptions | Loaded via `ITeacherAvailabilityRepository.GetTeacherExceptionsAsync` |
| Existing scheduled slots | Loaded via `ICourseScheduleRepository.GetScheduledSlotsAsync` |
| `effectiveStart` | `max(PreferredStartDate, today)` |
| `hardEndDate` | `PreferredEndDate` |

Output: `ScheduleGenerationResult { Slots, Conflicts, FitsInWindow }`.

### Per-candidate-date logic

```
for each session i in sessionList:
    slot = slots[i mod slots.Count]
    nextDate = first date >= cursor whose DayOfWeek == slot.DayOfWeekId

    while (nextDate, slot.TimeSlotId) is a Blocked exception   ← blackout: skip silently
        nextDate += 7 days       (capped at 52 attempts)

    if nextDate > hardEndDate                                   ← out of window: stop
        return result with FitsInWindow = false

    if (nextDate, slot.Id) ∈ existingScheduledSlots             ← hard conflict
        record Conflict { sessionNumber, date, slotId }
        // continue computing remaining sessions so caller sees ALL conflicts at once
    else
        emit ProposedScheduleSlot

    advance cursor (next slot in ring → same week; ring wrap → +1 day)
```

### Two kinds of "unavailable" — handled differently

| Cause | What it means | How the algorithm reacts |
|---|---|---|
| `TeacherAvailabilityException` (Blocked) | Teacher pre-declared a blackout (holiday, sick day) on a specific date+timeslot | **Skip silently** — advance one week and retry. The teacher's recurring offer is still real; this single date isn't. |
| Existing `Scheduled` `CourseSchedule` | Another student already paid for this exact (date, availability) | **Record as Conflict.** The caller (submit handler / pay handler) hard-rejects. |

This split keeps the weekly `TeacherAvailability` template stable (no flipping
`IsActive` on/off) while still preventing real double-bookings.

---

## API: Student-facing teacher availability calendar

**`GET /Api/V1/Student/Teachers/{teacherId}/Availability`**

Returns, for each date in the requested range, which of the teacher's weekly
slots are Free / Booked / Blocked. Used to render a calendar so the student
can pick a `PreferredStartDate` and `SelectedAvailabilityIds` that won't be
rejected at submit.

### Query parameters

| Name | Type | Default | Notes |
|---|---|---|---|
| `fromDate` | `date` (yyyy-MM-dd) | today | Optional |
| `toDate` | `date` | `fromDate + 30 days` | Optional. **Server caps** at `fromDate + 90 days`. |

### Authorization

`Student` or `Guardian` role.

### Sample request

```http
GET /Api/V1/Student/Teachers/7/Availability?fromDate=2026-04-28&toDate=2026-05-12
Authorization: Bearer <token>
```

If both query params are omitted:

```http
GET /Api/V1/Student/Teachers/7/Availability
```

…the server returns today through today + 30 days.

### Sample response

```json
{
  "data": {
    "teacherId": 7,
    "fromDate": "2026-04-28",
    "toDate": "2026-05-12",
    "days": [
      {
        "date": "2026-05-03",
        "dayOfWeekId": 1,
        "dayNameEn": "Sunday",
        "slots": [
          {
            "teacherAvailabilityId": 10,
            "timeSlotId": 3,
            "startTime": "16:00:00",
            "endTime": "17:00:00",
            "durationMinutes": 60,
            "labelEn": "Afternoon",
            "status": "Free"
          },
          {
            "teacherAvailabilityId": 12,
            "timeSlotId": 5,
            "startTime": "19:00:00",
            "endTime": "20:00:00",
            "durationMinutes": 60,
            "labelEn": "Evening",
            "status": "Booked"
          }
        ]
      },
      {
        "date": "2026-05-05",
        "dayOfWeekId": 3,
        "dayNameEn": "Tuesday",
        "slots": [
          {
            "teacherAvailabilityId": 11,
            "timeSlotId": 3,
            "startTime": "16:00:00",
            "endTime": "17:00:00",
            "durationMinutes": 60,
            "labelEn": "Afternoon",
            "status": "Blocked"
          }
        ]
      }
    ]
  },
  "succeeded": true,
  "statusCode": 200
}
```

### Status semantics

| Status | Meaning |
|---|---|
| `Free` | Bookable. The teacher has a recurring weekly slot here, no blackout, no existing booking. |
| `Booked` | A `CourseSchedule` with `Status = Scheduled` already exists on this `(Date, TeacherAvailabilityId)`. |
| `Blocked` | The teacher has a `Blocked` `TeacherAvailabilityException` on this `(Date, TimeSlotId)`. |

### Days are filtered

Days on which the teacher has **zero** matching weekly availabilities (e.g., the
teacher never works Fridays) are **omitted** from `days[]` — there's no point
returning empty rows. The client renders gaps between dates as "no availability."

---

## API: Updated request submission

**`POST /Api/V1/Student/EnrollmentRequests`**

Two new required fields in `data`:

```diff
 {
   "data": {
     "courseId": 1,
     "studentIds": [42],
     "invitedStudentIds": [],
     "selectedAvailabilityIds": [10, 11],
+    "preferredStartDate": "2026-05-15",
+    "preferredEndDate":   "2026-07-15",
     "notes": null,
     "proposedSessions": []
   }
 }
```

### Possible 400 responses

| Trigger | Message |
|---|---|
| `PreferredStartDate < today` | `"PreferredStartDate must be today or later."` |
| `PreferredEndDate < PreferredStartDate` | `"PreferredEndDate must be on or after PreferredStartDate."` |
| Window too short for N sessions | `"Selected slots can only fit X of Y sessions before {EndDate}. Extend the end date or add another availability slot."` |
| Existing booking on a candidate date | `"The following dates are already booked: session 2: 2026-05-17; … . Please pick a different start date or different availability slots."` |

### Successful response — new fields

```json
{
  "data": {
    "id": 123,
    "courseId": 1,
    "preferredStartDate": "2026-05-15",
    "preferredEndDate":   "2026-07-15",
    "proposedScheduleDates": [
      { "sessionNumber": 1, "date": "2026-05-17", "teacherAvailabilityId": 10, "durationMinutes": 60, "title": "Algebra Basics" },
      { "sessionNumber": 2, "date": "2026-05-19", "teacherAvailabilityId": 11, "durationMinutes": 60, "title": "Equations"      },
      { "sessionNumber": 3, "date": "2026-05-24", "teacherAvailabilityId": 10, "durationMinutes": 60, "title": "Functions"      },
      { "sessionNumber": 4, "date": "2026-05-26", "teacherAvailabilityId": 11, "durationMinutes": 60, "title": "Review"         }
    ],
    "...": "all existing fields stay"
  }
}
```

The teacher detail endpoint returns the same `proposedScheduleDates[]` so the
teacher reviews concrete dates before approving.

---

## Sample: end-to-end with conflicts

Setup:

- Teacher `id=7`, weekly availability: Sun 16:00 (slot 10), Tue 16:00 (slot 11).
- Student A submits a request with `[10, 11]`, `start=2026-05-03`, `end=2026-07-31`, 4-session course → **succeeds**, schedules become `[Sun May 3, Tue May 5, Sun May 10, Tue May 12]`. Pays. `CourseSchedule` rows now exist on those dates.
- Student B submits a request with `[10]`, `start=2026-05-03`, `end=2026-07-31`, 4-session course.

What happens:

```
Student B → POST /Api/V1/Student/EnrollmentRequests
        ↓
Submit handler computes candidates: Sun May 3, Sun May 10, Sun May 17, Sun May 24
        ↓
existingScheduledSlots loaded: { (May 3, 10), (May 10, 10) }   ← from Student A
        ↓
Preview returns:
  Slots:     [ session 3: Sun May 17 (10), session 4: Sun May 24 (10) ]
  Conflicts: [ session 1: Sun May 3 (10), session 2: Sun May 10 (10) ]
        ↓
Submit handler sees Conflicts.Count > 0 → 400 with message listing the conflicts
        ↓
Student B retries with start=2026-05-17 → Preview returns no Conflicts → request persists
```

If Student B's `start=2026-05-17` request *also* gets approved and reaches payment,
we re-check inside the transaction. If Student A had paid for those Sundays in the
meantime (impossible in this scenario but possible in general), the pay handler
rolls back its own (mock) `Payment` row and returns 400.

---

## Sample: end-to-end with a blackout

Setup:

- Teacher creates a Blocked `TeacherAvailabilityException` for `2026-05-17` on
  the 16:00 timeslot ("Eid").
- Student submits the same request as Student B above (`start=2026-05-03`).

What happens:

```
Submit handler computes candidates:
  cursor=May 3 → slot 10 (Sun) → May 3 → not blacked out → emit
  cursor=May 4 → slot 11 (Tue) → May 5 → not blacked out → emit  (then ring wraps, cursor=May 6)
  cursor=May 6 → slot 10 (Sun) → May 10 → not blacked out → emit
  cursor=May 11 → slot 11 (Tue) → May 12 → not blacked out → emit
  ⇒ all 4 sessions slotted normally, May 17 never even considered
```

If the course needed 6 sessions instead of 4 the algorithm would step over May 17:

```
  cursor=May 13 → slot 10 (Sun) → May 17 → BLACKED OUT → +7 days → May 24 → emit
  cursor=May 25 → slot 11 (Tue) → May 26 → emit
```

The student never sees the May 17 blackout; the blocked Sunday is simply not in
their `proposedScheduleDates[]`.

---

## API: Pay-time race-loser response

If two requests pass submit-time validation but only one beats the other to
payment:

```http
POST /Api/V1/Student/Payments/Enrollment
{
  "data": { "enrollmentId": 124 }
}
```

```json
{
  "data": null,
  "succeeded": false,
  "statusCode": 400,
  "message": "Some of your scheduled dates were just booked by another student. Please re-submit with different dates."
}
```

The mock `Payment` row is **not** persisted — the entire transaction rolls back.

---

## Schema changes

Two new columns on `CourseEnrollmentRequest`:

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `PreferredStartDate` | `date` | no (required) | Indexed for window queries |
| `PreferredEndDate`   | `date` | no (required) | |

Migration:

```bash
dotnet ef migrations add AddPreferredDatesToCourseEnrollmentRequest -p Qalam.Infrastructure -s Qalam.Api
dotnet ef database update -p Qalam.Infrastructure -s Qalam.Api
```

No change to `CourseSchedule` or `TeacherAvailability` — the algorithm uses the
existing indexes on `(Date)` and `(TeacherAvailabilityId)`.

---

## Files

**New**
- [Qalam.Core/Features/Student/Availability/Queries/GetTeacherAvailabilityByRange/](Qalam.Core/Features/Student/Availability/Queries/GetTeacherAvailabilityByRange/) — query + handler
- [Qalam.Infrastructure/Abstracts/ICourseScheduleRepository.cs](Qalam.Infrastructure/Abstracts/ICourseScheduleRepository.cs) + [Repositories/CourseScheduleRepository.cs](Qalam.Infrastructure/Repositories/CourseScheduleRepository.cs)
- DTOs in [Qalam.Data/DTOs/Course/EnrollmentDtos.cs](Qalam.Data/DTOs/Course/EnrollmentDtos.cs) (`ProposedScheduleSlotDto`) and [Qalam.Data/DTOs/Teacher/TeacherAvailabilityDtos.cs](Qalam.Data/DTOs/Teacher/TeacherAvailabilityDtos.cs) (`AvailabilitySlotStatus`, `AvailabilitySlotByDateDto`, `AvailabilityDayDto`, `TeacherAvailabilityByRangeDto`)

**Modified**
- [Qalam.Data/Entity/Course/CourseEnrollmentRequest.cs](Qalam.Data/Entity/Course/CourseEnrollmentRequest.cs) + [CourseEnrollmentRequestConfiguration.cs](Qalam.Infrastructure/Configurations/Course/CourseEnrollmentRequestConfiguration.cs) — date columns
- [Qalam.Service/Abstracts/IScheduleGenerationService.cs](Qalam.Service/Abstracts/IScheduleGenerationService.cs) + [Implementations/ScheduleGenerationService.cs](Qalam.Service/Implementations/ScheduleGenerationService.cs) — added `Preview(...)`
- [RequestCourseEnrollmentCommandHandler.cs](Qalam.Core/Features/Student/EnrollmentRequests/Commands/RequestCourseEnrollment/RequestCourseEnrollmentCommandHandler.cs) + [Validator.cs](Qalam.Core/Features/Student/EnrollmentRequests/Commands/RequestCourseEnrollment/RequestCourseEnrollmentCommandValidator.cs) — submit-time conflict detection
- [GetMyEnrollmentRequestByIdQueryHandler.cs](Qalam.Core/Features/Student/EnrollmentRequests/Queries/GetMyEnrollmentRequestById/GetMyEnrollmentRequestByIdQueryHandler.cs), [GetTeacherEnrollmentRequestByIdQueryHandler.cs](Qalam.Core/Features/Teacher/EnrollmentRequests/Queries/GetTeacherEnrollmentRequestById/GetTeacherEnrollmentRequestByIdQueryHandler.cs) — `proposedScheduleDates` in detail views
- [PayEnrollmentCommandHandler.cs](Qalam.Core/Features/Student/Payments/Commands/PayEnrollment/PayEnrollmentCommandHandler.cs), [PayGroupMemberCommandHandler.cs](Qalam.Core/Features/Student/Payments/Commands/PayGroupMember/PayGroupMemberCommandHandler.cs) — `effectiveStart` + race-loser check
- [Router.cs](Qalam.Data/AppMetaData/Router.cs), [StudentCourseController.cs](Qalam.Api/Controllers/Student/StudentCourseController.cs), [ModuleInfrastructureDependencies.cs](Qalam.Infrastructure/ModuleInfrastructureDependencies.cs)

---

## Out of scope

- A composite UNIQUE index on `CourseSchedule (Date, TeacherAvailabilityId, Status)` — would catch races at the DB level instead of in-handler. TODO.
- A "find me the next free slot" suggestion when submit fails on conflicts. Today the user re-tries with a different start date.
- `TeacherAvailabilityException` overlap with date *ranges* (current entity is single-date only).
- Caching the calendar response — the handler does 3 reads per request, fast enough without caching for now.
