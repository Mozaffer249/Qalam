# Student invitation detail

Simple contract for inbox → detail. Auth: Bearer, roles **Student** / **Guardian**.

## List

```http
GET /Api/V1/Student/Invitations?pageNumber=1&pageSize=10&scope=Active
```

Use **`invitationKey`** from each row. Do not send `source` as a query.

| Query | Values | Default |
|-------|--------|---------|
| `scope` | `Active` (pending/actionable) \| `Archived` (history) | `Active` |

| Example key | Meaning |
|-------------|---------|
| `EnrollmentRequest-901` | Course (S1) invite row 901 |
| `OpenSessionRequest-44` | OSR invite row 44 |

If `invitationKey` is missing, build `{source}-{invitationId}`.

| Caller | Rows (within `scope`) |
|--------|------|
| Invitee / guardian of invitee | One row per invite; `parentStatus` always set |
| Creator (`RequestedByUserId` / OSR `CreatedByGuardianId`) | One row per parent with `isOwner: true` |

Same parent as invitee + owner → keep the **invitee** row only. `isOwner` → no Accept/Reject; tap still opens detail. Terminal `parentStatus` (e.g. `Cancelled`) drives the badge for invitees too. `invitationId` on owner rows is the first **Pending** invite if any, else the first invite row.

Flutter Invitations tab: **dropdown** sends `scope` on each load (not client-side filter).

## Detail

```http
GET /Api/V1/Student/Invitations/{invitationKey}
```

- Malformed / bare int (`/Invitations/44`) → **400**
- Unknown key / no access / child opening own invite → **404**

Response includes `source`, **all members** (`invitedStudents` with `memberType` Own|Invited), `viewerStudentIds` (owned students on the request as Own or Invited), sessions + units, and CTAs.

## Who can open

| Caller | Open | `actionableStudentIds` |
|--------|------|------------------------|
| Owner | Yes | `[]` (no Accept) |
| Invited adult | Yes | self if Pending + in deadline |
| Guardian of invited child(ren) | Yes if any child is on the request | all their Pending children on this request |
| Child login | No | — |

## CTAs

Trust backend flags. Invitees never Pay.

| Flag | Use |
|------|-----|
| `canRespond` | Show Accept / Reject |
| `actionableStudentIds` | One respond POST **per** student id |
| `respondAcceptDecision` / `respondRejectDecision` | `Confirmed`/`Rejected` (S1) or `Accepted`/`Rejected` (OSR) |
| `canPay` / `canCancel` | Owner only |

Respond body: `{ "data": { "studentId": <id>, "decision": "<from detail>" } }`

| `source` | POST |
|----------|------|
| `EnrollmentRequest` | `/Api/V1/Student/EnrollmentRequests/{enrollmentRequestId}/Members/Response` |
| `OpenSessionRequest` | `/Api/V1/Student/OpenSessionRequests/{openSessionRequestId}/Members/Response` |

## Flutter

| Step | Where |
|------|--------|
| Inbox tap | `ActivityScreen` → `pushInvitationDetail(invitationKey)` (owner + invitee) |
| Screen | `InvitationDetailScreen` (`/invitations/:invitationKey`) |
| Load | `GET` `ApiEndpoints.invitationByKey` |
| Accept / Reject | Invitee only (`isOwner` / `canRespond`). Detail: `invitationDetailProvider.respondAll` |

Owner request screens (`EnrollmentRequestDetailScreen`, `OpenSessionRequestDetailScreen`) are unchanged.

Full API notes: [Student-Enrollment-Invitations-OSR-Frontend.md](./Student-Enrollment-Invitations-OSR-Frontend.md).