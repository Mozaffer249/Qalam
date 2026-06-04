# Teacher registration requirements — Admin & Teacher app guide

How to integrate **admin-controlled registration fields** in the SuperAdmin panel and the teacher mobile/web app.

> **Related:** API reference [`Teacher-Registration-Requirements.md`](Teacher-Registration-Requirements.md) · default seed [`seed-data/teacher-registration-requirements.json`](seed-data/teacher-registration-requirements.json) · SQL [`../scripts/seed-teacher-registration-requirements.sql`](../scripts/seed-teacher-registration-requirements.sql)  
> **Scalar / Swagger:** tags **Admin · Teacher registration requirements**, **Teacher Authentication**, **Teacher · Documents**

---

## Prerequisites (backend)

1. Apply migration `AddTeacherRegistrationRequirements`.
2. Seed default catalog (automatic on first API start, or run the SQL script).
3. Confirm four system rows exist: `identity_document`, `certificate`, `bio`, `location`.

---

## Teacher app — registration wizard

### Full flow

| Step | Screen | API |
|------|--------|-----|
| 0 | Splash / login | `GET /Api/V1/Authentication/Config` |
| 1 | Phone + email OTP | `POST …/Authentication/Teacher/LoginOrRegister` |
| 2 | Verify OTP | `POST …/Authentication/Teacher/VerifyOtp` |
| 3 | Name, email, password | `POST …/Authentication/Teacher/CompletePersonalInfo` |
| **4** | **Dynamic requirements** | **`GET …/Authentication/Teacher/RegistrationRequirements`** |
| **5** | **Submit answers** | **`POST …/Authentication/Teacher/SubmitRegistrationRequirements`** |
| 6 | Waiting for review | `GET /Api/V1/Teacher/TeacherDocuments/Status` |

Steps 0–3 are unchanged from the existing teacher auth flow (`docs/Auth-Config-Frontend.md`).

---

### Step 4 — Load requirements (before building the form)

```http
GET /Api/V1/Authentication/Teacher/RegistrationRequirements
```

No `Authorization` header required.

#### Response (`data`)

```json
{
  "requirements": [
    {
      "code": "identity_document",
      "nameAr": "وثيقة الهوية",
      "nameEn": "Identity document",
      "descriptionAr": "هوية وطنية أو إقامة أو جواز سفر حسب موقعك",
      "descriptionEn": "National ID, Iqama, or passport depending on location",
      "requirementType": "File",
      "isRequired": true,
      "sortOrder": 10,
      "minCount": 1,
      "maxCount": 1,
      "maxFileSizeBytes": 10485760,
      "allowedExtensions": [".pdf", ".jpg", ".jpeg", ".png"],
      "maxLength": null
    },
    {
      "code": "certificate",
      "requirementType": "File",
      "isRequired": true,
      "sortOrder": 20,
      "minCount": 1,
      "maxCount": 5,
      "maxFileSizeBytes": 10485760,
      "allowedExtensions": [".pdf", ".jpg", ".jpeg", ".png"]
    },
    {
      "code": "bio",
      "requirementType": "Text",
      "isRequired": false,
      "sortOrder": 30,
      "maxLength": 500
    },
    {
      "code": "location",
      "requirementType": "Boolean",
      "isRequired": true,
      "sortOrder": 40
    }
  ]
}
```

#### UI rules

1. Sort by `sortOrder` ascending.
2. Render by `requirementType`:

| Type | Widget | Labels |
|------|--------|--------|
| `File` | File picker(s) | `nameAr` / `nameEn`, `descriptionAr` / `descriptionEn` |
| `Text` | Multiline input | Enforce `maxLength` client-side |
| `Boolean` | Toggle / yes-no | e.g. “Teaching inside Saudi Arabia?” |

3. Show **Required** badge when `isRequired === true`.
4. For **File** items:
   - Allow `minCount` … `maxCount` files (certificates: 1–5).
   - Validate extension against `allowedExtensions` and size against `maxFileSizeBytes` before upload.
5. **Known system codes** map to fixed form fields (see submit table below). **Custom** file codes use `file_{code}`.

6. Optional: call `GET …/Authentication/IdentityTypes?isInSaudiArabia=true|false` when `identity_document` is active (after user answers location if it appears first — reorder in UI by `sortOrder` or ask location before identity metadata).

---

### Step 5 — Submit (multipart)

```http
POST /Api/V1/Authentication/Teacher/SubmitRegistrationRequirements
Authorization: Bearer {teacherJwt}
Content-Type: multipart/form-data
```

#### Map requirement `code` → form fields

| `code` | Type | Form fields |
|--------|------|-------------|
| `identity_document` | File | `identityType`, `documentNumber`, `issuingCountryCode`, `identityDocumentFile` |
| `certificate` | File | `certificates[0].file`, `certificates[0].title`, `certificates[0].issuer`, `certificates[0].issueDate` (repeat index for multiple) |
| `bio` | Text | `bio` |
| `location` | Boolean | `isInSaudiArabia` (`true` = inside KSA) |
| *any other file* | File | `file_{code}` e.g. `file_custom_cv` |

Only send fields for **active** requirements returned in step 4. Required items must be present; optional items can be omitted.

#### Example (minimal required set)

```
isInSaudiArabia: true
identityType: 1
documentNumber: "1234567890"
identityDocumentFile: (binary)
certificates[0].file: (binary)
certificates[0].title: "Bachelor of Education"
certificates[0].issuer: "University"
certificates[0].issueDate: "2020-06-01"
bio: "Experienced Quran teacher..."   // optional if bio.isRequired is false
```

#### Success

- Teacher status → `PendingVerification`.
- File rows → admin review **Pending**; text/boolean → **Approved** immediately (v1).
- Navigate to a “under review” screen; poll status (step 6).

#### Legacy endpoint

`POST …/Authentication/Teacher/UploadDocuments` — same behavior; keep only for old app builds.

---

### Step 6 — Track verification status

```http
GET /Api/V1/Teacher/TeacherDocuments/Status
Authorization: Bearer {teacherJwt}
```

#### Response (`data`)

```json
{
  "requirements": [
    {
      "code": "identity_document",
      "nameEn": "Identity document",
      "requirementType": "File",
      "isRequired": true,
      "isSubmitted": true,
      "verificationStatus": "Pending",
      "rejectionReason": null,
      "teacherDocumentId": 42
    },
    {
      "code": "certificate",
      "requirementType": "File",
      "isRequired": true,
      "isSubmitted": true,
      "verificationStatus": "Pending"
    },
    {
      "code": "bio",
      "requirementType": "Text",
      "isRequired": false,
      "isSubmitted": true,
      "verificationStatus": "Approved",
      "textValue": "Experienced Quran teacher..."
    },
    {
      "code": "location",
      "requirementType": "Boolean",
      "isRequired": true,
      "isSubmitted": true,
      "verificationStatus": "Approved",
      "boolValue": true
    }
  ],
  "legacyDocuments": [ "...existing document DTOs..." ]
}
```

#### UI mapping

| `verificationStatus` | Teacher UI |
|---------------------|------------|
| `Pending` | Yellow / “Under review” |
| `Approved` | Green / checkmark |
| `Rejected` | Red + show `rejectionReason`; enable re-upload via `PUT …/Teacher/TeacherDocuments/{teacherDocumentId}/Reupload` |

Account becomes **Active** when every **active required** requirement is **Approved** (teacher may still see individual pending items until admin finishes).

---

## Admin app — SuperAdmin catalog

**Role:** `SuperAdmin` only  
**Base URL:** `/Api/V1/Admin/TeacherRegistrationRequirements`

### List catalog (settings screen)

```http
GET /Api/V1/Admin/TeacherRegistrationRequirements
Authorization: Bearer {superAdminJwt}
```

Shows **all** rows (active + inactive). Use for a table with columns: code, name, type, required, active, sort order, system flag.

### Toggle visibility (quick action)

```http
PATCH /Api/V1/Admin/TeacherRegistrationRequirements/{id}/active
Content-Type: application/json

{ "isActive": false }
```

Prefer this over delete for system rows (`isSystem: true`).

### Create custom requirement

```http
POST /Api/V1/Admin/TeacherRegistrationRequirements
Content-Type: application/json
```

```json
{
  "code": "custom_cv",
  "nameAr": "السيرة الذاتية",
  "nameEn": "CV",
  "descriptionAr": "PDF فقط",
  "descriptionEn": "PDF only",
  "requirementType": 1,
  "isActive": true,
  "isRequired": false,
  "sortOrder": 50,
  "minCount": 0,
  "maxCount": 1,
  "maxFileSizeBytes": 10485760,
  "allowedExtensions": [".pdf"],
  "mapsToDocumentType": 3
}
```

**Enums**

| Field | Values |
|-------|--------|
| `requirementType` | `1` File, `2` Text, `3` Boolean |
| `mapsToDocumentType` | `1` Identity, `2` Certificate, `3` Other (file types only) |

**Rules**

- `code` — unique, lowercase snake_case, stable (used in teacher submit as `file_{code}`).
- Do not delete system codes; use `isActive: false` to hide.
- Delete fails if submissions exist.

### Update requirement

```http
PUT /Api/V1/Admin/TeacherRegistrationRequirements/{id}
```

Body: `UpdateTeacherRegistrationRequirementDto` — labels, `isActive`, `isRequired`, sort, min/max, file limits, `maxLength`. **Code cannot change.**

### Recommended admin UI

```
┌─────────────────────────────────────────────────────────────┐
│ Teacher registration requirements              [+ Add]      │
├──────────┬──────────┬────────┬──────────┬────────┬────────┤
│ Code     │ Name     │ Type   │ Required │ Active │ Order  │
├──────────┼──────────┼────────┼──────────┼────────┼────────┤
│ identity │ Identity │ File   │ ✓        │ ✓      │ 10     │  🔒 system
│ cert…    │ Certs    │ File   │ ✓        │ ✓      │ 20     │  🔒 system
│ bio      │ Bio      │ Text   │          │ ✓      │ 30     │  🔒 system
│ location │ Location │ Bool   │ ✓        │ ✓      │ 40     │  🔒 system
│ custom_cv│ CV       │ File   │          │ ✓      │ 50     │  [Edit][Delete]
└──────────┴──────────┴────────┴──────────┴────────┴────────┘
```

Edit drawer fields by type:

- **File:** min/max count, extensions, max size, maps-to document type  
- **Text:** max length  
- **Boolean:** labels only  

---

## Admin app — Review pending teachers

**Role:** `SuperAdmin` or `Admin`  
**Base:** `/Api/V1/Admin/TeacherManagement`

### Pending list

```http
GET /Api/V1/Admin/TeacherManagement/Pending?pageNumber=1&pageSize=10
```

### Teacher detail (checklist + documents)

```http
GET /Api/V1/Admin/TeacherManagement/{teacherId}
```

Use **`registrationRequirements`** for the checklist and **`canBeActivated`** for the activate button state:

```json
{
  "teacherId": 12,
  "fullName": "Ahmed Ali",
  "status": "PendingVerification",
  "registrationRequirements": [
    {
      "code": "identity_document",
      "isRequired": true,
      "isSubmitted": true,
      "verificationStatus": "Pending",
      "teacherDocumentId": 42
    }
  ],
  "documents": [ "..." ],
  "canBeActivated": false
}
```

Show checklist above document viewer. `canBeActivated === true` when all **required** items are **Approved**.

### Approve / reject document

Still per **document** (links to submission automatically):

```http
POST /Api/V1/Admin/TeacherManagement/{teacherId}/Documents/{documentId}/Approve
POST /Api/V1/Admin/TeacherManagement/{teacherId}/Documents/{documentId}/Reject
Content-Type: application/json

{ "reason": "Document is blurry, please re-upload" }
```

After approve/reject, refresh teacher detail — `registrationRequirements` and `canBeActivated` update. Teacher status becomes:

- **Active** — all active required submissions approved  
- **DocumentsRejected** — any required file rejected  
- **PendingVerification** — still pending review  

---

## Dynamic form builder (teacher) — pseudocode

```typescript
type Requirement = {
  code: string;
  requirementType: 'File' | 'Text' | 'Boolean';
  isRequired: boolean;
  minCount: number;
  maxCount: number;
  maxFileSizeBytes: number;
  allowedExtensions: string[];
  maxLength?: number;
  nameAr: string;
  nameEn: string;
};

function renderRequirement(req: Requirement) {
  switch (req.requirementType) {
    case 'File':
      if (req.code === 'identity_document') return <IdentityDocumentSection required={req.isRequired} />;
      if (req.code === 'certificate') return <CertificateList min={req.minCount} max={req.maxCount} />;
      return <GenericFileUpload name={`file_${req.code}`} {...req} />;
    case 'Text':
      if (req.code === 'bio') return <BioField maxLength={req.maxLength} required={req.isRequired} />;
      return null; // v1: only bio text field
    case 'Boolean':
      if (req.code === 'location') return <LocationToggle required={req.isRequired} />;
      return null;
  }
}

async function submit(form: FormData, requirements: Requirement[]) {
  // Append only fields for active codes present in requirements[]
  await fetch('/Api/V1/Authentication/Teacher/SubmitRegistrationRequirements', {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
    body: form,
  });
}
```

---

## Checklist — implementation done when

### Teacher app

- [ ] Step 4 calls `RegistrationRequirements` and builds UI from response (not hardcoded fields).
- [ ] Submit uses `SubmitRegistrationRequirements` multipart with correct field names per `code`.
- [ ] Client validates file count, extension, size, and text length from API limits.
- [ ] Post-submit status screen uses `TeacherDocuments/Status` checklist.
- [ ] Re-upload uses `teacherDocumentId` from status when `Rejected`.

### Admin app

- [ ] SuperAdmin settings page: list / create / edit / toggle active for requirements.
- [ ] System rows show lock icon; delete disabled.
- [ ] Teacher review page shows `registrationRequirements` checklist + `canBeActivated`.
- [ ] Approve/reject still uses existing document endpoints.

---

## Troubleshooting

| Issue | Check |
|-------|--------|
| **500 — `Invalid object name 'teacher.TeacherRegistrationRequirements'`** | Migration not applied. See [Database setup](#database-setup) below. |
| Empty requirements list | Migration applied but seed missing — run `scripts/seed-teacher-registration-requirements.sql` or restart API after migration succeeds. |
| Submit 400 “No active requirements” | Catalog empty or all inactive |
| Teacher stuck Pending | Admin must approve all **required** **File** items |
| Custom file ignored | Form field must be `file_{code}` exactly |
| Old mobile app | Still works via `UploadDocuments` wrapper |

### Database setup

1. **Apply EF migrations** (creates tables + runs startup seeder):

```bash
dotnet ef database update --project Qalam.Infrastructure --startup-project Qalam.Api
```

Or restart Docker — `Program.cs` runs `MigrateAsync()` on boot.

2. **If migration fails on Scenario2** (`SessionOfferId` duplicate / `SessionRequests` already exists):

Your DB may have an older migration id (`20260523151604_Scenario2_…`) while the repo uses `20260523200422_Scenario2_…`. Run:

```bash
# sqlcmd or Azure Data Studio
scripts/repair-scenario2-migration.sql
dotnet ef database update --project Qalam.Infrastructure --startup-project Qalam.Api
```

3. **Emergency manual apply** (tables + seed only):

```bash
scripts/apply-teacher-registration-requirements.sql
scripts/seed-teacher-registration-requirements.sql
```
