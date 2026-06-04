# Teacher registration requirements (admin-controlled)

SuperAdmins configure which fields teachers must complete during registration. The teacher app loads **active** requirements before the documents step and submits answers in one multipart request.

> **Frontend integration:** [`Teacher-Registration-Requirements-Frontend.md`](Teacher-Registration-Requirements-Frontend.md) — step-by-step for admin panel and teacher app.

## Data model

| Table | Schema | Purpose |
|-------|--------|---------|
| `TeacherRegistrationRequirements` | `teacher` | Catalog (code, type, labels, active/required, validation) |
| `TeacherRegistrationSubmissions` | `teacher` | Per teacher × requirement (status, text/bool, link to `TeacherDocument`) |

**Seeded codes** (system, do not delete):

| Code | Type | Maps to |
|------|------|---------|
| `identity_document` | File | `TeacherDocument` (Identity) |
| `certificate` | File | `TeacherDocument` (Certificate), min 1 max 5 |
| `bio` | Text | `Teacher.Bio` (auto-approved on submit) |
| `location` | Boolean | `Teacher.Location` (auto-approved on submit) |

## Teacher flow

1. `GET /Api/V1/Authentication/Config` — OTP/email settings (unchanged).
2. OTP → personal info (unchanged).
3. **`GET /Api/V1/Authentication/Teacher/RegistrationRequirements`** — `AllowAnonymous`, active requirements only.
4. **`POST /Api/V1/Authentication/Teacher/SubmitRegistrationRequirements`** — `multipart/form-data`, Teacher JWT.

Legacy: **`POST /Api/V1/Authentication/Teacher/UploadDocuments`** — same handler (obsolete).

### Submit body (examples)

| Field | When |
|-------|------|
| `isInSaudiArabia` | Required when `location` is active |
| `bio` | When `bio` is active |
| `identityType`, `documentNumber`, `issuingCountryCode`, `identityDocumentFile` | When `identity_document` is active |
| `certificates[i].file`, title, issuer, dates | When `certificate` is active |
| `file_{code}` | Custom **File** requirements (e.g. `file_custom_cv`) |

### Registration requirements response

```json
{
  "requirements": [
    {
      "code": "identity_document",
      "type": "File",
      "required": true,
      "nameAr": "...",
      "nameEn": "...",
      "minCount": 1,
      "maxCount": 1,
      "allowedExtensions": [".pdf", ".jpg"],
      "maxFileSizeBytes": 10485760
    }
  ]
}
```

## Teacher status

`GET /Api/V1/Teacher/TeacherDocuments/Status` returns:

- `requirements` — checklist per active requirement
- `legacyDocuments` — existing document rows (backward compatible)

## Admin

### CRUD catalog (SuperAdmin)

Base: `/Api/V1/Admin/TeacherRegistrationRequirements`

| Method | Path | Action |
|--------|------|--------|
| GET | `/` | List all (including inactive) |
| GET | `/{id}` | Detail |
| POST | `/` | Create custom requirement |
| PUT | `/{id}` | Update |
| DELETE | `/{id}` | Delete (not system, no submissions) |
| PATCH | `/{id}/active` | Toggle `isActive` |

### Review

- Approve/reject **documents** via existing teacher management endpoints.
- Linked `TeacherRegistrationSubmission` rows sync from document status.
- Teacher becomes **Active** when every **active required** submission is **Approved** (see `ITeacherRegistrationCompletionService`).

`GET` teacher details (admin) includes `registrationRequirements` checklist and `canBeActivated` derived from required items when configured.

## Activation rules

- **File** requirements with `maxCount > 1`: need at least `minCount` submissions; any rejection → `DocumentsRejected`; all approved → counts toward activation.
- **Text/Boolean** seeded items are marked **Approved** on submit (admin reviews files only in v1).

## Deployment

Apply migration `AddTeacherRegistrationRequirements`:

```bash
dotnet ef database update --project Qalam.Infrastructure --startup-project Qalam.Api
```

Seeder runs on app startup (`TeacherRegistrationRequirementsSeeder`) and backfills submissions for teachers in `PendingVerification` / `Active`.

## Out of scope (v1)

- Re-validating existing **Active** teachers when a new required field is added.
- Student/parent registration requirements.
- Moving name/password into the requirements catalog.
