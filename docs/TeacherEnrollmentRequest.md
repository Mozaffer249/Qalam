# Teacher Enrollment Requests — API Guide

Endpoints for a teacher to view, approve, and reject student enrollment requests on their courses.

- Base route: `/Api/V1/Teacher/EnrollmentRequests`
- Auth: `Authorization: Bearer <teacher-jwt>` (role `Teacher`).
- All responses follow the standard envelope:

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": "...",
  "errors": null,
  "meta": null
}
```

## Status enum

| Value | Name      |
| ----- | --------- |
| 1     | Pending   |
| 2     | Approved  |
| 3     | Rejected  |
| 4     | Cancelled |

---

## 1. List enrollment requests for a course

`GET /Api/V1/Teacher/EnrollmentRequests`

Returns a paginated list of enrollment requests for a course owned by the authenticated teacher.

### Query parameters

| Name         | Type   | Required | Notes                                         |
| ------------ | ------ | -------- | --------------------------------------------- |
| `CourseId`   | int    | yes      | Course must belong to the authenticated teacher. |
| `Status`     | int    | no       | Filter by status (1-4). Omit for all statuses. |
| `PageNumber` | int    | no       | Default `1`.                                  |
| `PageSize`   | int    | no       | Default `10`.                                 |

### Example

```http
GET /Api/V1/Teacher/EnrollmentRequests?CourseId=1&Status=1&PageNumber=1&PageSize=10
```

### Response (200)

`data` is a flat list of items, pagination metadata in `meta`.

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": [
    {
      "id": 12,
      "courseId": 1,
      "courseTitle": "Mathematics - Grade 10",
      "requestedByUserName": "Parent Muzafar",
      "status": 1,
      "createdAt": "2026-04-20T10:15:33.123",
      "totalMinutes": 540,
      "estimatedTotalPrice": 450,
      "groupMemberCount": 6,
      "teachingModeNameEn": "In-Person",
      "sessionTypeNameEn": "Group"
    }
  ],
  "errors": null,
  "meta": {
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

### Errors

- `404` — teacher profile not found, or the course does not belong to you.

---

## 2. Get enrollment request details

`GET /Api/V1/Teacher/EnrollmentRequests/{id}`

Returns the full details of a single enrollment request, including group members and proposed sessions.

### Example

```http
GET /Api/V1/Teacher/EnrollmentRequests/12
```

### Response (200)

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "id": 12,
    "courseId": 1,
    "courseTitle": "Mathematics - Grade 10",
    "requestedByUserName": "Parent Muzafar",
    "status": 1,
    "createdAt": "2026-04-20T10:15:33.123",
    "totalMinutes": 540,
    "estimatedTotalPrice": 450,
    "teachingModeNameEn": "In-Person",
    "sessionTypeNameEn": "Group",
    "notes": "Prefers afternoon sessions.",
    "rejectionReason": null,
    "selectedAvailabilityIds": [11, 12, 14],
    "groupMembers": [
      {
        "studentId": 5,
        "studentName": "Ahmad Muzafar",
        "memberType": 1,
        "confirmationStatus": 2,
        "confirmedAt": "2026-04-20T11:02:00.000"
      },
      {
        "studentId": 8,
        "studentName": "Sara Muzafar",
        "memberType": 2,
        "confirmationStatus": 2,
        "confirmedAt": "2026-04-20T12:30:00.000"
      }
    ],
    "proposedSessions": [
      { "sessionNumber": 1, "durationMinutes": 60, "title": "Algebra basics", "notes": null },
      { "sessionNumber": 2, "durationMinutes": 60, "title": "Equations",       "notes": null }
    ]
  },
  "errors": null,
  "meta": null
}
```

### Field notes

- `memberType`: `1 = Own` (the requester's own student), `2 = Invited`.
- `confirmationStatus`: `1 = Pending`, `2 = Confirmed`, `3 = Rejected`.
- `proposedSessions` is empty for flexible courses.

### Errors

- `404` — request not found, or it does not belong to one of your courses.

---

## 3. Approve an enrollment request

`POST /Api/V1/Teacher/EnrollmentRequests/{id}/Approve`

No request body. Approving:

1. Verifies the request belongs to one of your courses and is `Pending`.
2. Requires at least one group member with `confirmationStatus = Confirmed`.
3. Creates the enrollment(s):
   - **Group session type** -> one `CourseGroupEnrollment` plus a `Member` row per confirmed member, all `PendingPayment`.
   - **Individual session type** -> a single `CourseEnrollment` for the confirmed student, `PendingPayment`.
4. Sets a payment deadline (configured server-side, e.g. 48 hours).
5. Marks the request `Approved`.

### Example

```http
POST /Api/V1/Teacher/EnrollmentRequests/12/Approve
```

### Response (200)

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": "Enrollment request approved. Payment deadline: 2026-04-22 10:15:33Z",
  "errors": null,
  "meta": null
}
```

### Errors

- `404` — teacher profile not found, request not found.
- `400`:
  - `"This request does not belong to your course."`
  - `"Only pending requests can be approved."`
  - `"No confirmed group members found."`

---

## 4. Reject an enrollment request

`POST /Api/V1/Teacher/EnrollmentRequests/{id}/Reject`

Marks a `Pending` request as `Rejected` and stores an optional reason on the request.

### Request body

```json
{
  "rejectionReason": "Course is full for this term."
}
```

| Field             | Type   | Required | Rules                |
| ----------------- | ------ | -------- | -------------------- |
| `rejectionReason` | string | no       | Max length **500**.  |

You may also send `{}` if you don't want to provide a reason.

### Example

```http
POST /Api/V1/Teacher/EnrollmentRequests/12/Reject
Content-Type: application/json

{ "rejectionReason": "Course is full for this term." }
```

### Response (200)

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": "Enrollment request rejected.",
  "errors": null,
  "meta": null
}
```

### Errors

- `404` — teacher profile not found, request not found.
- `400`:
  - `"This request does not belong to your course."`
  - `"Only pending requests can be rejected."`

---

## Quick reference

| Action  | Method | Path                                                  | Body                          |
| ------- | ------ | ----------------------------------------------------- | ----------------------------- |
| List    | GET    | `/Api/V1/Teacher/EnrollmentRequests?CourseId=...`     | -                             |
| Details | GET    | `/Api/V1/Teacher/EnrollmentRequests/{id}`             | -                             |
| Approve | POST   | `/Api/V1/Teacher/EnrollmentRequests/{id}/Approve`     | -                             |
| Reject  | POST   | `/Api/V1/Teacher/EnrollmentRequests/{id}/Reject`      | `{ "rejectionReason": "..." }`|
