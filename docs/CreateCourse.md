# Create Course — Frontend Guide

A short, practical guide for the frontend to create a course from a teacher account.

---

## TL;DR

- **Endpoint:** `POST /Api/V1/Teacher/TeacherCourse`
- **Auth:** `Authorization: Bearer <teacher-jwt>` (role must be `Teacher`)
- **Content-Type:** `application/json`
- Two modes:
  - **Non-flexible** — course has a defined session plan. Send `sessions[]` in the order you want.
  - **Flexible** — no fixed plan. Don't send `sessions` and don't send `sessionDurationMinutes`.
- **You never send `sessionNumber`.** Just push items into the `sessions` array in the order you want them displayed. The server numbers them `1..N` based on that order.

Minimal non-flexible payload:

```json
{
  "title": "Mathematics - Grade 10",
  "description": "Full year algebra & geometry program.",
  "teacherSubjectId": 12,
  "teachingModeId": 1,
  "sessionTypeId": 2,
  "isFlexible": false,
  "sessionDurationMinutes": 60,
  "price": 75.00,
  "maxStudents": 6,
  "canIncludeInPackages": true,
  "sessions": [
    { "durationMinutes": 60, "title": "Intro & diagnostic", "notes": null },
    { "durationMinutes": 60, "title": "Linear equations",   "notes": null },
    { "durationMinutes": 90, "title": "Quadratics",         "notes": "Bring calculator." }
  ]
}
```

---

## Data flow

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant API as "Qalam API"
    participant DB as Database

    FE->>API: "POST /Api/V1/Teacher/TeacherCourse<br/>sessions: [s1, s2, s3]"
    API->>API: "Assign sessionNumber = index + 1"
    API->>DB: "Insert Course + CourseSessions (1,2,3)"
    DB-->>API: "Course with ids"
    API-->>FE: "200 OK<br/>sessions: [{sessionNumber:1,...}, {sessionNumber:2,...}, ...]"
```

---

## Two modes — what to send

### Non-flexible course (fixed plan)

The teacher knows the whole session outline up-front.

- `isFlexible`: `false`
- `sessionDurationMinutes`: required, > 0 (default duration for the course)
- `sessions`: required, non-empty. Each item = one session in display order.

### Flexible course (on-demand)

Sessions are booked as needed; no plan at creation time.

- `isFlexible`: `true`
- `sessionDurationMinutes`: `null` / omit it
- `sessions`: `null` / omit it

---

## Request fields

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `title` | string (≤ 200) | yes | |
| `description` | string (≤ 2000) | no | |
| `teacherSubjectId` | int (> 0) | yes | One of the teacher's active subjects (from their profile). |
| `teachingModeId` | int (> 0) | yes | From the teaching-modes lookup (Online / In-Person). |
| `sessionTypeId` | int (> 0) | yes | From the session-types lookup (Individual / Group). |
| `isFlexible` | bool | yes | Drives the rules for `sessionDurationMinutes` and `sessions`. |
| `sessionDurationMinutes` | int? (> 0) | **required if `isFlexible=false`**; must be `null` if `isFlexible=true` | |
| `price` | decimal (≥ 0) | yes | Hourly rate. |
| `maxStudents` | int? (≥ 2) | **required (≥ 2) for Group sessions**; must be `null` for Individual | |
| `canIncludeInPackages` | bool | no | |
| `sessions` | array | **required and non-empty if `isFlexible=false`**; must be empty/null if `isFlexible=true` | See below. |

### `sessions[]` item

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `durationMinutes` | int (> 0) | yes | Per-session duration. Can differ from `sessionDurationMinutes`. |
| `title` | string (≤ 150) | no | |
| `notes` | string (≤ 500) | no | |

> Do NOT send `sessionNumber` (or any order/index field). The array order **is** the session order. The server will assign `sessionNumber = 1, 2, 3, ...` following your array order.

---

## Examples

### A) Non-flexible group course

**Request**

```http
POST /Api/V1/Teacher/TeacherCourse
Authorization: Bearer <teacher-jwt>
Content-Type: application/json
```

```json
{
  "title": "Algebra Intensive",
  "description": "3-session intensive review.",
  "teacherSubjectId": 12,
  "teachingModeId": 1,
  "sessionTypeId": 2,
  "isFlexible": false,
  "sessionDurationMinutes": 60,
  "price": 75.00,
  "maxStudents": 6,
  "canIncludeInPackages": true,
  "sessions": [
    { "durationMinutes": 60, "title": "Intro",      "notes": null },
    { "durationMinutes": 60, "title": "Equations",  "notes": null },
    { "durationMinutes": 90, "title": "Quadratics", "notes": null }
  ]
}
```

**Response — `200 OK`**

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "id": 42,
    "title": "Algebra Intensive",
    "description": "3-session intensive review.",
    "isActive": true,
    "teacherId": 7,
    "teacherDisplayName": "Ali Hassan",
    "domainId": 2,
    "domainNameEn": "STEM",
    "teacherSubjectId": 12,
    "subjectNameEn": "Mathematics",
    "curriculumId": 3,
    "curriculumNameEn": "National",
    "levelId": 4,
    "levelNameEn": "Secondary",
    "gradeId": 10,
    "gradeNameEn": "Grade 10",
    "teachingModeId": 1,
    "teachingModeNameEn": "In-Person",
    "sessionTypeId": 2,
    "sessionTypeNameEn": "Group",
    "isFlexible": false,
    "sessionsCount": 3,
    "sessionDurationMinutes": 60,
    "price": 75.00,
    "maxStudents": 6,
    "canIncludeInPackages": true,
    "status": "Published",
    "units": null,
    "sessions": [
      { "id": 101, "sessionNumber": 1, "durationMinutes": 60, "title": "Intro",      "notes": null },
      { "id": 102, "sessionNumber": 2, "durationMinutes": 60, "title": "Equations",  "notes": null },
      { "id": 103, "sessionNumber": 3, "durationMinutes": 90, "title": "Quadratics", "notes": null }
    ]
  },
  "errors": null,
  "meta": null
}
```

### B) Flexible individual course

**Request**

```json
{
  "title": "On-demand Tutoring",
  "description": "Book sessions as needed.",
  "teacherSubjectId": 12,
  "teachingModeId": 2,
  "sessionTypeId": 1,
  "isFlexible": true,
  "sessionDurationMinutes": null,
  "price": 40.00,
  "maxStudents": null,
  "canIncludeInPackages": false,
  "sessions": null
}
```

**Response highlights**

```json
{
  "data": {
    "isFlexible": true,
    "sessionsCount": null,
    "sessionDurationMinutes": null,
    "sessions": null
  }
}
```

---

## Response shape the UI renders from

Everything the UI needs for the course page comes back in `data` (a `CourseDetailDto`). Key pieces:

- `id`, `title`, `description`
- Display labels: `teacherDisplayName`, `subjectNameEn`, `teachingModeNameEn`, `sessionTypeNameEn`, `domainNameEn`, `curriculumNameEn`, `levelNameEn`, `gradeNameEn`
- `price`, `maxStudents`, `canIncludeInPackages`
- `status` = `"Published"`, `isActive` = `true`
- **Sessions block**
  - `sessionsCount`: number for non-flexible, `null` for flexible. (It equals `sessions.length`; you can use either.)
  - `sessionDurationMinutes`: default duration; `null` for flexible.
  - `sessions`: array ordered by `sessionNumber` ascending. Use `sessionNumber` for labels, e.g. `Session {{sessionNumber}}`.

Minimal session-list render contract:

```ts
type CourseSession = {
  id: number;
  sessionNumber: number; // server-assigned, 1..N
  durationMinutes: number;
  title: string | null;
  notes: string | null;
};
```

---

## Errors the UI should surface

All errors come back with `succeeded: false` and a `message` / `errors[]`. Most will be `400 Bad Request`.

Validation errors (driven by input):

| When | Message | Suggested UI |
| --- | --- | --- |
| `isFlexible=false` and `sessions` missing/empty | `Sessions are required when course is not flexible.` | Inline error on the Sessions list. |
| `isFlexible=true` and `sessions` provided | `Sessions must be empty when course is flexible.` | Hide / clear the list when user toggles to flexible. |
| `isFlexible=true` and `sessionDurationMinutes` set | `SessionDurationMinutes must be null when course is flexible.` | Clear the duration field on toggle. |
| Any session item with `durationMinutes <= 0` | `'Duration Minutes' must be greater than '0'.` | Inline error on that row. |
| Any session item with `title.length > 150` | `'Title' must be 150 characters or fewer.` | Inline error on that row. |
| Any session item with `notes.length > 500` | `'Notes' must be 500 characters or fewer.` | Inline error on that row. |

Business-rule errors (driven by the teacher's account / lookups):

| When | Message |
| --- | --- |
| The `teacherSubjectId` is not one of the teacher's active subjects | `Invalid subject selection. Please select a subject from your active teaching subjects.` |
| Group session without capacity | `MaxStudents is required and must be >= 2 for group courses.` |
| Individual session with capacity | `MaxStudents must be null for individual courses.` |
| Non-flexible without `sessionDurationMinutes` | `SessionDurationMinutes is required when course is not flexible.` |
| Invalid lookup ids | `Invalid TeachingModeId.` / `Invalid SessionTypeId.` |

`401 Unauthorized` — missing/invalid token, or the account is not a teacher.

---

## Reordering / editing sessions later

This endpoint creates the course **and** its sessions in one shot. Editing or reordering sessions after creation is not supported here — it needs a separate endpoint. For now, if the teacher wants to change the session plan, re-create the course.

---

## cURL (for quick manual tests)

```bash
curl -X POST "https://<host>/Api/V1/Teacher/TeacherCourse" \
  -H "Authorization: Bearer <teacher-jwt>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Algebra Intensive",
    "teacherSubjectId": 12,
    "teachingModeId": 1,
    "sessionTypeId": 2,
    "isFlexible": false,
    "sessionDurationMinutes": 60,
    "price": 75.00,
    "maxStudents": 6,
    "canIncludeInPackages": true,
    "sessions": [
      {"durationMinutes":60,"title":"Intro","notes":null},
      {"durationMinutes":60,"title":"Equations","notes":null},
      {"durationMinutes":90,"title":"Quadratics","notes":null}
    ]
  }'
```

---

## What happens on the server (reference only)

- Input is validated (`CreateCourseCommandValidator`).
- The course is inserted with `Status = Published`, `IsActive = true`.
- For each item in your `sessions` array (in order), a `CourseSession` row is created with `SessionNumber = index + 1`.
- `sessionsCount` is derived at read time from `sessions.length` for non-flexible courses; it's not stored as a column.
- Responses always return `sessions` ordered by `sessionNumber` ascending — i.e. the same order you sent them in.
