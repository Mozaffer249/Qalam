# Enrollment Request Business Flow

## Overview

The enrollment system connects students with teachers through a multi-step request, approval, and payment flow. It supports both individual and group courses, guardian-managed children, and invited external students.

---

## Actors

| Actor | Description |
|-------|-------------|
| **Student (Adult)** | Has their own student profile, can enroll themselves |
| **Guardian** | Parent/guardian who manages minor children's enrollments |
| **Both** | User who is both a student AND a guardian — can enroll self + children |
| **Invited Student** | External student invited to join a group enrollment |
| **Teacher** | Course owner who approves/rejects enrollment requests |

---

## Step-by-Step Flow

### 1. Student Submits Enrollment Request

**Endpoint:** `POST /Api/V1/Student/EnrollmentRequests`

The requesting user provides:
- **CourseId** — the course to enroll in
- **StudentIds** — their own students (self and/or children). At least one required.
- **InvitedStudentIds** (optional) — external students to invite (group courses only)
- **SelectedAvailabilityIds** — preferred teacher time slots
- **ProposedSessions** (flexible courses only) — custom session structure
- **Notes** (optional)

**Validations:**
- All `StudentIds` must belong to the requesting user (own profile or guardian's children)
- `InvitedStudentIds` cannot overlap with own students
- Course must be Published and Active
- No duplicate pending request for same user + course
- Individual courses: exactly 1 student, no invites
- Group courses: total students <= MaxStudents
- TeachingModeId is auto-derived from the course (not passed by the client)

**Result:**
- `CourseEnrollmentRequest` created with `Status = Pending`
- Own students added as `GroupMembers` with `MemberType = Own`, auto-confirmed
- Invited students added with `MemberType = Invited`, `ConfirmationStatus = Pending`

---

### 2. Invited Students Accept/Reject Invitations

**Endpoint:** `POST /Api/V1/Student/EnrollmentRequests/{id}/Members/Response`

**View pending invitations:** `GET /Api/V1/Student/Invitations`
- Returns all pending invitations for the user's students (self + children if guardian)

**Authorization rules for responding:**
- **Minor student** (`IsMinor = true`) — ONLY the guardian can accept/reject
- **Non-minor student** (`IsMinor = false`) — ONLY the student themselves can accept/reject

**Result:** `ConfirmationStatus` updated to `Confirmed` or `Rejected`

---

### 3. Teacher Reviews Enrollment Requests

**List requests:** `GET /Api/V1/Teacher/EnrollmentRequests?CourseId=1&Status=Pending`
- Requests are fetched **per course** (CourseId is required)
- Optional filter by Status (Pending, Approved, Rejected, Cancelled)
- Shows: requester name, group member count, estimated price, teaching mode

**View detail:** `GET /Api/V1/Teacher/EnrollmentRequests/{id}`
- Full request details including:
  - Group members with names, types (Own/Invited), and confirmation status
  - Selected teacher availability slots
  - Proposed sessions (for flexible courses)
  - Notes from the student

---

### 4. Teacher Approves or Rejects

#### Approve
**Endpoint:** `POST /Api/V1/Teacher/EnrollmentRequests/{id}/Approve`

**For individual courses:**
- Creates `CourseEnrollment` with `EnrollmentStatus = PendingPayment`
- Sets `PaymentDeadline` (default: 48 hours from approval)
- Records `ApprovedByTeacherId` and `ApprovedAt`

**For group courses:**
- Creates `CourseGroupEnrollment` with `Status = PendingPayment`
- Creates `CourseGroupEnrollmentMember` for each confirmed group member
- Each member has `PaymentStatus = Pending`
- Sets `PaymentDeadline` on the group enrollment

**Request status** changes to `Approved`

#### Reject
**Endpoint:** `POST /Api/V1/Teacher/EnrollmentRequests/{id}/Reject`

- Request status changes to `Rejected`
- Optional `RejectionReason` stored (max 500 chars)

---

### 5. Student Pays Within Deadline

After approval, students have a configurable window (default **48 hours**) to complete payment.

**Configuration** (`appsettings.json`):
```json
"EnrollmentSettings": {
  "PaymentDeadlineHours": 48,
  "ExpirationCheckIntervalMinutes": 5
}
```

---

### 6. Automatic Expiration (Background Service)

A background service (`EnrollmentExpirationService`) runs every 5 minutes and checks for enrollments past their `PaymentDeadline`:

- **Individual enrollments** with `PendingPayment` + expired deadline → `Cancelled`
- **Group enrollments** with `PendingPayment` + expired deadline → `Cancelled`
- The original request stays as `Approved` (teacher's decision is preserved)
- If the student wants to re-enroll, they must submit a new request

---

## Status Lifecycle

### Enrollment Request Status
```
Pending → Approved (by teacher)
Pending → Rejected (by teacher)
Pending → Cancelled (by student — not yet implemented)
```

### Enrollment Status (after approval)
```
PendingPayment → Active (after payment)
PendingPayment → Cancelled (payment deadline expired)
Active → Completed (course finished)
Active → Cancelled (manual cancellation)
```

### Group Member Confirmation Status
```
Pending → Confirmed (accepted invitation)
Pending → Rejected (declined invitation)
```

---

## Entity Relationships

```
CourseEnrollmentRequest (Pending)
├── RequestedByUser (the user who created the request)
├── Course
├── GroupMembers[]
│   ├── Student (Own — auto-confirmed)
│   └── Student (Invited — needs acceptance)
├── SelectedAvailabilities[]
└── ProposedSessions[] (flexible courses)

         ↓ Teacher Approves ↓

CourseEnrollment (Individual)          CourseGroupEnrollment (Group)
├── Student                            ├── LeaderStudent
├── ApprovedByTeacher                  ├── EnrollmentRequest
├── PaymentDeadline                    ├── PaymentDeadline
└── EnrollmentStatus                   └── Members[]
                                           ├── Student + PaymentStatus
                                           └── Student + PaymentStatus
```

---

## API Endpoints Summary

### Student Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/Student/EnrollmentRequests` | Submit enrollment request |
| GET | `/Student/EnrollmentRequests` | List my requests (paginated) |
| GET | `/Student/EnrollmentRequests/{id}` | Request detail |
| GET | `/Student/Invitations` | Pending invitations for self + children |
| POST | `/Student/EnrollmentRequests/{id}/Members/Response` | Accept/reject invitation |

### Teacher Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/Teacher/EnrollmentRequests?CourseId=X` | List requests for a course |
| GET | `/Teacher/EnrollmentRequests/{id}` | Request detail with student names |
| POST | `/Teacher/EnrollmentRequests/{id}/Approve` | Approve (creates enrollment + deadline) |
| POST | `/Teacher/EnrollmentRequests/{id}/Reject` | Reject with optional reason |
