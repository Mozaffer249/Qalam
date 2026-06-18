# Teacher Profile & Settings — Frontend Implementation Guide

UX-first walkthrough of every screen, section, and tap a logged-in teacher needs after registration. For the registration journey (phone OTP → Verify → Personal info → Documents), see [TEACHER_AUTH_FRONTEND_GUIDE.md](TEACHER_AUTH_FRONTEND_GUIDE.md).

All requests below require `Authorization: Bearer <teacher-jwt>`. All responses are wrapped in the standard envelope:

```json
{ "statusCode": "OK", "succeeded": true, "message": "Success", "data": { ... }, "errors": null, "meta": null }
```

---

## Top-level navigation

The settings entry point is a single home screen. The header shows the teacher's name and current status pill. Below the header, four tiles are arranged in a 2×2 grid: **Documents**, **Subjects & Units**, **Availability**, and **Account & Security**. The first three are fully functional today; the fourth is greyed out because the backend doesn't expose those endpoints yet — see §5 Coming soon. A **Log out** button sits at the bottom of the screen.

### Where the header data comes from

There's no canonical `GET /Profile` endpoint wired today. Cache the values from the most recent auth response:

- `firstName`, `lastName`, `email`, `phoneNumber` come from `data.account` returned by `POST /Authentication/Teacher/CompletePersonalInfo`, or decode them from the JWT claims.
- `status` is derived from the most recent `nextStep.nextStepName` returned by `POST /Authentication/Teacher/VerifyOtp`:
  - `"Upload Documents"` → "Awaiting documents" pill.
  - `"Add Teaching Subjects and Units"` → subjects step during registration (before admin approval).
  - `"Awaiting Admin Verification"` → docs and/or subjects under review.
  - `"Re-upload Rejected Documents"` → "Documents rejected" pill.
  - `"Set Your Availability"` or `"Registration Complete"` → "Active" pill.

The Documents / Subjects / Availability count badges on the tiles come from the lightweight GET calls described in each section below.

### Log out

Single tap fires `POST /Api/V1/Authentication/Logout`, clears the stored JWT, and returns the user to the phone-OTP entry screen.

---

## 1. Documents screen

Opens when the teacher taps **Documents** on the home screen. Renders every file the teacher has uploaded, grouped by document type. Each row shows the document's metadata and a status pill. Rejected rows also show the admin's rejection reason and a Re-upload button.

### Sections

The list is split into three groups, ordered top-to-bottom:

| Section | Filter | Empty-state copy |
|---|---|---|
| **Identity** | `documentType === 1` — exactly one row | (impossible — teacher must have one to land here) |
| **Certificates** | `documentType === 2` — 1 to 5 rows | "No certificates uploaded." |
| **Other** | `documentType === 3` | "No other documents uploaded." |

### Per-row layout (prose)

Each row shows: an icon for the document type, the title (certificate title for certificates, identity-type label for identity docs, or "Document" for Other), the relevant metadata line below the title (document number for identity, issuer + issue date for certificates), the submission date, and on the right a colored status pill. When the pill is "Rejected", a warning row appears below with the rejection reason and a **Re-upload** button.

### Taps

| Tap | API call | Then… |
|---|---|---|
| Screen load | `GET /Api/V1/Teacher/TeacherDocuments/Status` | Render the list grouped by `documentType` |
| **View** | none — open `filePath` in browser / preview component | — |
| **Re-upload** (only visible when `verificationStatus === 3 Rejected`) | `PUT /Api/V1/Teacher/TeacherDocuments/{id}/Reupload` (multipart, field `file`) | Show success toast, re-fetch the status list, flip pill to "Under review" |

### `GET /Api/V1/Teacher/TeacherDocuments/Status` — sample response

```json
{
  "data": [
    {
      "id": 17,
      "documentType": 1,
      "filePath": "/uploads/teachers/42/identity/abc.pdf",
      "verificationStatus": 2,
      "rejectionReason": null,
      "reviewedAt": "2026-05-20T09:15:00Z",
      "documentNumber": "1234567890",
      "identityType": 1,
      "issuingCountryCode": null,
      "certificateTitle": null,
      "issuer": null,
      "issueDate": null,
      "createdAt": "2026-05-15T08:00:00Z"
    },
    {
      "id": 18,
      "documentType": 2,
      "filePath": "/uploads/teachers/42/certificates/xyz.pdf",
      "verificationStatus": 3,
      "rejectionReason": "Scan is too blurry — please re-upload a clearer copy.",
      "reviewedAt": "2026-05-20T09:18:00Z",
      "certificateTitle": "BSc Mathematics",
      "issuer": "King Saud University",
      "issueDate": "2018-06-01",
      "createdAt": "2026-05-15T08:00:00Z"
    }
  ]
}
```

### Status pill mapping

| `verificationStatus` | Pill label | Show alongside |
|:---:|---|---|
| `1 Pending` | Under review | nothing |
| `2 Approved` | Approved | `reviewedAt` |
| `3 Rejected` | Rejected | `rejectionReason` + **Re-upload** button |

### Errors

| HTTP | Trigger | What to render |
|---|---|---|
| 401 | Token expired | Redirect to phone-OTP login |
| 404 | `"Teacher profile not found"` | Inline empty state: *"We can't find your teacher profile. Please contact support."* |
| 400 | Re-upload attempted on a non-rejected document | Toast with `message` |

### Re-upload sheet

Tapping **Re-upload** opens a sheet with a file picker. Accepted file types: `.pdf, .jpg, .jpeg, .png`. Max size 25 MB (matches the document-validation rules from registration). Submit triggers the PUT call above.

---

## 2. Subjects & Units screen

Opens when the teacher taps **Subjects & Units** on the home screen. The teacher manages what they teach — picking from the platform's subject catalog, scoping to specific content units, and (for Quran) attaching content-type and level specializations per unit.

### Sections

The screen header shows the title "Subjects & Units" and a **+ Add Subject** button on the right. The body is a vertical list of cards, one per subject the teacher offers. Each card has:

- A subject icon and name (English + Arabic).
- A summary line: either "Teaches the full subject" OR "N units selected".
- A status pill from `isActive` + `verificationStatus`: **Pending review** (`verificationStatus: Pending`), **Active** (`isActive: true`, `verificationStatus: Approved`), **Inactive** (`isActive: false`, still approved), or **Rejected** (`verificationStatus: Rejected`). When rejected, show `rejectionReason` and optional `reviewedAt` below the pill (same pattern as documents in §1).
- An expandable **View units** chevron at the bottom. When expanded, units appear as a vertical list with each unit's name and, for Quran units, two small badges showing the Quran content type (Memorization / Recitation / Tajweed) and the level (Noorani / Beginner / Intermediate / Advanced — or "All levels" when not specified).

### Taps

| Tap | API call | Notes |
|---|---|---|
| Screen load | `GET /Api/V1/Teacher/TeacherSubject` | Returns **all** subjects (active, inactive, rejected) — render the cards |
| **+ Add Subject** | Opens the filter-options wizard | See [docs/Teacher-Availability-and-Subjects.md → Education filter options](docs/Teacher-Availability-and-Subjects.md) for the wizard's full flow. It ends with `POST /Api/V1/Teacher/TeacherSubject` (additive — duplicates skipped) |
| **View units** (chevron) | none — local toggle | Units come back with the GET above |
| **Remove subject** | Not exposed today | Server stub returns *"Not implemented yet"*. Hide or disable the affordance |

Full request / response schemas and the Quran content-type and level tables live in [docs/Teacher-Availability-and-Subjects.md](docs/Teacher-Availability-and-Subjects.md). No duplication here.

### Empty state

When `data` is an empty array, replace the list with: *"You haven't added any subjects yet. Pick subjects from the catalog to start receiving session requests."* Place a single **+ Add Subject** button below the message.

---

## 3. Availability screen

Opens when the teacher taps **Availability** on the home screen. Shows the recurring weekly schedule and per-date exceptions (blocked days or extra slots outside the weekly pattern).

### Sections

The screen has three vertical sections, in order:

1. **Date range picker** — two date inputs (From, To) and an **Apply** button. Defaults to today through today + 90 days. Drives the query params on the GET below.
2. **Weekly schedule** — header text "Weekly schedule" with a **+ Add slots** button on the right. Below the header, seven day rows (Sunday through Saturday in that order). Each row lists time-slot chips for that day, or the text "(no slots)" when empty. Each chip shows the slot's start–end time.
3. **Exceptions** — header text "Exceptions in selected range" with a **+ Add exception** button on the right. Below the header, a vertical list of exception rows. Each row shows the date, the slot's start–end time, an "Extra" or "Blocked" badge, the optional reason text, and a **Remove** button on the right.

### Taps

| Tap | API call | Notes |
|---|---|---|
| Screen load + **Apply** | `GET /Api/V1/Teacher/TeacherAvailability?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD` | Render both sections from `data.weeklySchedule` and `data.exceptions` |
| **+ Add slots** | Opens a sheet with day-of-week picker + time-slot multi-select → on Save: `POST /Api/V1/Teacher/TeacherAvailability` (additive — duplicates skipped) | See [docs/Teacher-Availability-and-Subjects.md → Teacher availability](docs/Teacher-Availability-and-Subjects.md) for the request body |
| **Remove a slot from the weekly grid** | Not exposed today — disable or hide the affordance | Data-layer method exists but no endpoint. Operator workaround only |
| **+ Add exception** | Opens a sheet: date picker + time-slot picker + Blocked/Extra toggle + optional reason → `POST /Api/V1/Teacher/TeacherAvailability/exceptions` | Body: `{ date, timeSlotId, exceptionType: "Blocked" | "Extra", reason? }` |
| **Remove** on an exception row | `DELETE /Api/V1/Teacher/TeacherAvailability/exceptions/{id}` | Optimistic remove from local state; re-fetch on error |

### Lookup endpoints for the pickers

| What | Endpoint |
|---|---|
| Available time slots | `GET /Api/V1/Teaching/TimeSlots` |
| Day-of-week labels | `GET /Api/V1/Teaching/DaysOfWeek` |

### Empty state

When `data.weeklySchedule` is empty, replace the Weekly schedule section's day rows with: *"No weekly schedule yet. Add the days and time slots you can teach so students can book sessions."* Place a **+ Add slots** button below the message. When `data.exceptions` is empty, the Exceptions section just shows the **+ Add exception** button with no list rows.

---

## 4. Cross-screen UI rules

| Rule | Why |
|---|---|
| Show the teacher's status pill on every settings screen header | Several screens behave differently when the teacher is `Blocked`. Even read-only screens should warn the user. The status comes from the cached `nextStep.nextStepName` — see "Where the header data comes from" above |
| Disable POST / PUT / DELETE on **Availability** and **Courses** when the teacher is `PendingVerification`, `AwaitingDocuments`, or `Blocked` | Those flows require `Status == Active` |
| Allow **+ Add Subject** during `AwaitingDocuments`, `PendingVerification`, and `DocumentsRejected` | Subjects are reviewed before account activation; `POST /Teacher/TeacherSubject` is allowed unless `Blocked` |
| On any 401 response from any endpoint here, redirect to phone-OTP login | The JWT has lapsed; the teacher must re-verify |
| Re-upload sheet must restrict file types to `.pdf, .jpg, .jpeg, .png` and max 25 MB | Matches the document-validation rules from registration |

---

## 5. Coming soon — Account & Security

The fourth tile on the home screen is greyed out today. Tapping it opens a sheet listing the planned features, each marked "Coming soon", so designers can wireframe the future and FE can ship the entry point now. The sheet is organized into four sub-sections:

#### Personal info

- Edit name, phone, profile picture — Coming soon.
- Edit email — Coming soon.

#### Password

- Change password — Coming soon.
- Forgot password (works today via reset code) — live link, see below.

#### Security

- Active sessions — Coming soon.
- Trusted devices — Coming soon.
- Security events — Coming soon.

#### Account

- Export my data — Coming soon.
- Delete account — Coming soon.

### Why it's "Coming soon"

All the rows above have route constants declared in [`Router.cs`](Qalam.Data/AppMetaData/Router.cs) (`AccountGetProfile`, `AccountUpdateProfile`, `AccountChangeEmail`, `AuthenticationChangePassword`, `AccountGetSessions`, `AccountGetTrustedDevices`, `AccountGetSecurityEvents`, `AccountExportData`, `AccountDelete`, etc.) but no controller action wires them up. Calling these paths today returns 404. Half the MediatR handlers exist already (`GetProfileQueryHandler`, `UpdateProfileCommandHandler`, `ChangePasswordCommandHandler`, `GetActiveSessionsQueryHandler`, `GetSecurityEventsQueryHandler`, `GetTrustedDevicesQueryHandler`); the other half (TerminateSession, TrustDevice, ChangeEmail, ExportData, Delete) need both handler and controller.

### The only one that works today: Forgot password

Route the "Forgot password (works today via reset code)" row to the existing reset flow:

1. `POST /Api/V1/Authentication/SendResetPasswordCode` with `{ "email": "..." }` — server emails a code.
2. `POST /Api/V1/Authentication/ResetPassword` with `{ "email": "...", "code": "...", "newPassword": "..." }` — server sets the new password.

Both endpoints are wired. Use them as the password-change fallback until `POST /Authentication/ChangePassword` is exposed.

---

## 6. Enums reference (for status pills and badges)

### TeacherStatus

| Value | Name | UI |
|:---:|---|---|
| 1 | `AwaitingDocuments` | "Awaiting documents" — disable Subjects / Availability |
| 2 | `PendingVerification` | "Pending verification" — disable Subjects / Availability |
| 3 | `DocumentsRejected` | "Documents rejected" — push to Documents tile |
| 4 | `Active` | "Active" — everything enabled |
| 5 | `Blocked` | "Blocked — contact support" — disable everything except Log out |

### DocumentVerificationStatus

| Value | Name | Pill behavior |
|:---:|---|---|
| 1 | `Pending` | "Under review" |
| 2 | `Approved` | "Approved" + show `reviewedAt` |
| 3 | `Rejected` | "Rejected" + show `rejectionReason` + **Re-upload** button |

### TeacherDocumentType

| Value | Name |
|:---:|---|
| 1 | `IdentityDocument` |
| 2 | `Certificate` |
| 3 | `Other` |

### IdentityType (populated on identity-type documents)

| Value | Name | Used when |
|:---:|---|---|
| 1 | `NationalId` | Inside Saudi Arabia |
| 2 | `Iqama` | Inside Saudi Arabia |
| 3 | `Passport` | Outside Saudi Arabia — `issuingCountryCode` required |
| 4 | `DrivingLicense` | Outside Saudi Arabia — `issuingCountryCode` required |

### AvailabilityExceptionType

| Value | Name | UI |
|:---:|---|---|
| 1 | `Blocked` | "Blocked" — teacher unavailable that date/slot |
| 2 | `Extra` | "Extra" — teacher available outside the normal weekly pattern |

---

## Error envelope (any endpoint on this page)

```json
{
  "statusCode": "BadRequest",
  "succeeded": false,
  "message": "Teacher profile not found",
  "data": null,
  "errors": null,
  "meta": null
}
```

| HTTP | UX |
|---|---|
| 400 | Inline toast with `message` |
| 401 | Redirect to phone-OTP login |
| 403 | "You don't have permission" — usually means JWT lacks the Teacher role; tell user to re-login |
| 404 | Empty state on the relevant section |

---

## Related docs

- [TEACHER_AUTH_FRONTEND_GUIDE.md](TEACHER_AUTH_FRONTEND_GUIDE.md) — registration onboarding (everything before this screen exists)
- [docs/Teacher-Availability-and-Subjects.md](docs/Teacher-Availability-and-Subjects.md) — full API reference for the Subjects and Availability screens
- [docs/Admin-Teacher-Subjects-Frontend.md](docs/Admin-Teacher-Subjects-Frontend.md) — admin subject review (inactivate / reject / restore) and status fields on teacher GET
- [docs/Teacher-Registration.md](docs/Teacher-Registration.md) — server-side flow notes for registration
- [docs/Teacher-Quran-Specialization-Design.md](docs/Teacher-Quran-Specialization-Design.md) — Quran content-type and level reference for the units picker
