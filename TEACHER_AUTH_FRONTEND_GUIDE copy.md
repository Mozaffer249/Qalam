# Teacher Authentication - Frontend Implementation Guide

## Overview

Multi-step registration flow: Phone OTP -> Personal Info -> Upload Documents -> Admin Verification -> Add Subjects -> Set Availability -> Dashboard.

**Base URL:** `Api/V1/Authentication/Teacher/` (registration) and `Api/V1/Teacher/` (post-approval).

All responses are wrapped in:

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": { ... },
  "errors": null,
  "meta": null
}
```

---

## Registration Flow Diagram

```
Screen 1: Phone Input
    POST /Authentication/Teacher/LoginOrRegister
        |
Screen 2: OTP Verification
    POST /Authentication/Teacher/VerifyOtp → returns token + NextStep
        |
        ├── Existing user (registration complete) → Dashboard
        |
Screen 3: Personal Info
    POST /Authentication/Teacher/CompletePersonalInfo  (Bearer token required)
        |                                                → returns NEW token (with Teacher role)
        |
Screen 4: Upload Documents
    POST /Authentication/Teacher/UploadDocuments  (Bearer token + Teacher role, multipart/form-data)
        |
        → status becomes PendingVerification (await admin review)
        |
        ├── If admin rejects → status DocumentsRejected → re-upload via /Teacher/TeacherDocuments/{id}/Reupload
        └── If admin approves → status Active
        |
Screen 5: Add Teaching Subjects (post-approval, Active status)
    POST /Teacher/TeacherSubject  (Bearer token + Teacher role)
        |
Screen 6: Set Weekly Availability
    POST /Teacher/TeacherAvailability  (Bearer token + Teacher role)
        |
        → Dashboard (Registration Complete)
```

**Important: Token updates on key steps.** When a response contains a non-null `token`, replace the stored token. After `CompletePersonalInfo`, the new token carries the `Teacher` role claim — without it, document upload and all post-approval endpoints will return 401.

---

## Step 1: Send OTP (Login or Register)

**`POST /Api/V1/Authentication/Teacher/LoginOrRegister`**

No auth required. Works for both new and existing users — the response tells you which path to follow.

### Request

```typescript
interface SendPhoneOtpRequest {
  countryCode: string;  // e.g. "+966" — regex: ^\+[0-9]{1,4}$ (default: "+966")
  phoneNumber: string;  // e.g. "501234567" — regex: ^[0-9]{9,15}$
}
```

### Request Sample

```json
{
  "countryCode": "+966",
  "phoneNumber": "501234567"
}
```

### Response Sample

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "isNewUser": true,
    "message": "OTP sent successfully. Welcome! You will create a new account.",
    "phoneNumber": "*******4567"
  },
  "errors": null,
  "meta": null
}
```

```typescript
interface SendOtpResponseData {
  isNewUser: boolean;       // true = registration path, false = login path
  message: string;
  phoneNumber: string;      // masked: "*******4567"
}
```

### Frontend Logic

- Show phone input with country code dropdown (default "+966")
- On success: navigate to OTP screen, store `isNewUser` to show "Register" vs "Sign In" context
- Blocked accounts return 400 with "Your account has been blocked. Please contact support."

---

## Step 2: Verify OTP

**`POST /Api/V1/Authentication/Teacher/VerifyOtp`**

No auth required.

### Request

```typescript
interface VerifyOtpRequest {
  phoneNumber: string;  // same phone from step 1 (without country code)
  otpCode: string;      // 4-digit OTP (test code: "1234")
}
```

### Request Sample

```json
{
  "phoneNumber": "501234567",
  "otpCode": "1234"
}
```

### Response Sample (New User)

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "isNewUser": true,
    "nextStep": {
      "currentStep": 2,
      "nextStep": 3,
      "nextStepName": "Complete Personal Information",
      "isRegistrationComplete": false,
      "message": null,
      "rejectedDocuments": null
    }
  },
  "errors": null,
  "meta": null
}
```

### Response Sample (Existing User, Registration Complete)

```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "isNewUser": false,
    "nextStep": {
      "currentStep": 6,
      "nextStep": 0,
      "nextStepName": "Registration Complete",
      "isRegistrationComplete": true,
      "message": "Your profile is complete! You can now start accepting students.",
      "rejectedDocuments": null
    }
  }
}
```

```typescript
interface VerifyOtpResponseData {
  token: string;            // JWT — store and use as Bearer for subsequent calls
  isNewUser: boolean;
  nextStep: RegistrationStep;
}

interface RegistrationStep {
  currentStep: number;            // last completed step
  nextStep: number;               // next step number (0 if complete or awaiting verification)
  nextStepName: string;           // human-readable: see "Next Step Names" below
  isRegistrationComplete: boolean;
  message: string | null;         // user-facing guidance (e.g., "Your documents are being reviewed")
  rejectedDocuments: RejectedDocumentInfo[] | null;  // only populated when DocumentsRejected
}

interface RejectedDocumentInfo {
  documentId: number;
  documentType: number;           // 1=IdentityDocument, 2=Certificate, 3=Other
  rejectionReason: string;
}
```

### Next Step Names (drives routing)

| nextStepName | Meaning | Frontend Route |
|--------------|---------|----------------|
| "Complete Personal Information" | Need to submit name/email/password | `/register/personal-info` |
| "Upload Documents" | Personal info done, need to upload ID + certificates | `/register/upload-documents` |
| "Awaiting Admin Verification" | Docs uploaded, waiting for review | `/register/pending` |
| "Re-upload Rejected Documents" | Some docs rejected — show `rejectedDocuments` list | `/register/reupload` |
| "Add Teaching Subjects and Units" | Approved! Pick subjects | `/register/subjects` |
| "Set Your Availability" | Subjects set, configure weekly schedule | `/register/availability` |
| "Registration Complete" | All done | `/dashboard` |

### Frontend Logic

```typescript
const { data } = response.data;

if (data.token) {
  localStorage.setItem('token', data.token);
}

navigateByNextStep(data.nextStep.nextStepName);
```

---

## Step 3: Complete Personal Information

**`POST /Api/V1/Authentication/Teacher/CompletePersonalInfo`**

Requires: `Authorization: Bearer <token>` (from VerifyOtp).

### Request

```typescript
interface CompletePersonalInfoRequest {
  firstName: string;   // required, max 50 chars
  lastName: string;    // required, max 50 chars
  email?: string;      // required when provided; must be valid email format
  password: string;    // min 8 chars + uppercase + lowercase + digit + special
}
```

> Note: `userId` is auto-populated from the JWT — do NOT send it.

### Request Sample

```json
{
  "firstName": "Ahmed",
  "lastName": "Al-Saud",
  "email": "ahmed.teacher@example.com",
  "password": "SecurePass@123"
}
```

### Response Sample

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "account": {
      "firstName": "Ahmed",
      "lastName": "Al-Saud",
      "email": "ahmed.teacher@example.com",
      "phoneNumber": "+966501234567",
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    },
    "nextStep": {
      "currentStep": 3,
      "nextStep": 4,
      "nextStepName": "Upload Documents",
      "isRegistrationComplete": false,
      "message": null,
      "rejectedDocuments": null
    }
  }
}
```

**Important:** This response returns a **NEW token** under `data.account.token` that now includes the `Teacher` role claim. You MUST replace the stored token — without this token, `/UploadDocuments` will return 401.

### Validation Rules

| Field | Rule |
|-------|------|
| `firstName` | Required, max 50 chars |
| `lastName` | Required, max 50 chars |
| `email` | Required, valid email format |
| `password` | Min 8 chars, must contain: 1 uppercase `[A-Z]`, 1 lowercase `[a-z]`, 1 digit `[0-9]`, 1 special char `[\W_]` |

### Frontend Logic

```typescript
const { data } = response.data;
localStorage.setItem('token', data.account.token);  // REPLACE token with new one carrying Teacher role
navigate('/register/upload-documents');
```

Show a password-strength indicator that checks all 5 conditions live.

---

## Step 4: Upload Documents

**`POST /Api/V1/Authentication/Teacher/UploadDocuments`**

Requires: `Authorization: Bearer <token>` (from CompletePersonalInfo) + Role: `Teacher`.

**Content-Type:** `multipart/form-data` — this endpoint accepts file uploads.

### Request (multipart/form-data fields)

| Field | Type | Notes |
|-------|------|-------|
| `IsInSaudiArabia` | boolean | Drives which identity types are allowed |
| `IdentityType` | int | `1`=NationalId, `2`=Iqama, `3`=Passport, `4`=DrivingLicense |
| `DocumentNumber` | string | Required |
| `IssuingCountryCode` | string \| null | Required for Passport/DrivingLicense; must be empty for NationalId/Iqama |
| `IdentityDocumentFile` | file | Required — single identity document file |
| `Certificates[0].File` | file | At least 1, max 5 certificate files |
| `Certificates[0].Title` | string \| null | Optional |
| `Certificates[0].Issuer` | string \| null | Optional |
| `Certificates[0].IssueDate` | string \| null | Optional, "YYYY-MM-DD" |
| `Certificates[1].File` | file | ... and so on |

### Validation Rules

| Rule | Detail |
|------|--------|
| `DocumentNumber` | Required, non-empty |
| `IdentityDocumentFile` | Required |
| If `IsInSaudiArabia = true` | `IdentityType` must be NationalId (1) or Iqama (2) |
| If `IsInSaudiArabia = false` | `IdentityType` must be Passport (3) or DrivingLicense (4) |
| If Passport or DrivingLicense | `IssuingCountryCode` is required |
| If NationalId or Iqama | `IssuingCountryCode` must be empty |
| Certificates count | Min 1, max 5 |
| Each `Certificates[i].File` | Required |

### Identity Types Reference

| Value | Name | Allowed When |
|-------|------|--------------|
| 1 | NationalId | `IsInSaudiArabia = true` |
| 2 | Iqama | `IsInSaudiArabia = true` |
| 3 | Passport | `IsInSaudiArabia = false` |
| 4 | DrivingLicense | `IsInSaudiArabia = false` |

Use **`GET /Api/V1/Authentication/IdentityTypes?isInSaudiArabia=true`** (or `false`) to fetch the dropdown options pre-filtered server-side.

### Response Sample

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Documents uploaded successfully. Your account is awaiting verification.",
  "data": "Documents uploaded successfully.",
  "errors": null,
  "meta": null
}
```

After this call, the teacher's status becomes `PendingVerification`. Re-call `/VerifyOtp` or check `nextStep` via login to see status transitions.

### Frontend Logic (FormData example)

```typescript
const formData = new FormData();
formData.append('IsInSaudiArabia', 'true');
formData.append('IdentityType', '1');                 // NationalId
formData.append('DocumentNumber', '1234567890');
formData.append('IdentityDocumentFile', identityFile);

certificates.forEach((cert, i) => {
  formData.append(`Certificates[${i}].File`, cert.file);
  if (cert.title)     formData.append(`Certificates[${i}].Title`, cert.title);
  if (cert.issuer)    formData.append(`Certificates[${i}].Issuer`, cert.issuer);
  if (cert.issueDate) formData.append(`Certificates[${i}].IssueDate`, cert.issueDate);
});

await api.post('/Api/V1/Authentication/Teacher/UploadDocuments', formData, {
  headers: { 'Content-Type': 'multipart/form-data' }
});

navigate('/register/pending');
```

---

## Step 5: Pending Verification & Document Re-upload

After upload, the teacher is in `PendingVerification`. An admin reviews each document and either approves the teacher (→ `Active`) or rejects specific documents (→ `DocumentsRejected`).

### Check Document Status

**`GET /Api/V1/Teacher/TeacherDocuments/Status`**

Requires: `Authorization: Bearer <token>` + Role: `Teacher`.

Returns a list of all uploaded documents with their current status (Pending / Approved / Rejected + rejection reason).

```typescript
interface TeacherDocumentReview {
  documentId: number;
  documentType: number;          // 1=IdentityDocument, 2=Certificate, 3=Other
  status: number;                // 1=Pending, 2=Approved, 3=Rejected
  rejectionReason: string | null;
  // ... + file metadata
}
```

### Re-upload a Rejected Document

**`PUT /Api/V1/Teacher/TeacherDocuments/{documentId}/Reupload`**

Requires: `Authorization: Bearer <token>` + Role: `Teacher`.

**Content-Type:** `multipart/form-data`.

| Field | Type | Notes |
|-------|------|-------|
| `file` | file | New document file |

Only documents in `Rejected` status can be re-uploaded. After re-upload, the document goes back to `Pending`.

### Frontend Logic

- Poll `GET /TeacherDocuments/Status` (or re-call `/VerifyOtp` on app re-open) to detect transitions
- When `nextStep.nextStepName === "Re-upload Rejected Documents"`, render the `rejectedDocuments` list with rejection reasons and a re-upload button per item

---

## Step 6: Add Teaching Subjects (post-approval)

**`POST /Api/V1/Teacher/TeacherSubject`**

Requires: `Authorization: Bearer <token>` + Role: `Teacher`. Only available after admin approval (status = `Active`).

This is a **batch replace** — sending the array replaces all existing subjects for the teacher.

### Request

```typescript
interface SaveTeacherSubjectsRequest {
  subjects: TeacherSubjectItem[];
}

interface TeacherSubjectItem {
  subjectId: number;
  canTeachFullSubject: boolean;     // true = whole subject, false = specific units only
  units: TeacherSubjectUnit[];      // required when canTeachFullSubject = false
}

interface TeacherSubjectUnit {
  unitId: number;
  quranContentTypeId?: number | null;  // Quran only: 1=Memorization, 2=Recitation, 3=Tajweed; null = all
  quranLevelId?: number | null;        // Quran only: 1=Noorani, 2=Beginner, 3=Intermediate, 4=Advanced; null = all
}
```

### Request Sample (Quran teacher)

```json
{
  "subjects": [
    {
      "subjectId": 499,
      "canTeachFullSubject": false,
      "units": [
        { "unitId": 115, "quranContentTypeId": 1, "quranLevelId": null },
        { "unitId": 116, "quranContentTypeId": 1, "quranLevelId": 2 }
      ]
    }
  ]
}
```

### Response

Returns `TeacherSubjectsResponseDto` with full details (subject names AR/EN, unit names, Quran type/level translations).

### Lookup Endpoints

- `GET /Api/V1/Subjects` — all subjects
- `GET /Api/V1/Subjects/Grade/{gradeId}` — subjects for a grade
- `GET /Api/V1/Subjects/Domain/{domainId}` — subjects for a domain
- `GET /Api/V1/Content/Units` — content units

---

## Step 7: Set Weekly Availability

**`POST /Api/V1/Teacher/TeacherAvailability`**

Requires: `Authorization: Bearer <token>` + Role: `Teacher`.

Adds new day/slot combinations to the existing schedule (skips duplicates).

### Request

```typescript
interface SaveTeacherAvailabilityRequest {
  daySchedules: DayAvailability[];
}

interface DayAvailability {
  dayOfWeekId: number;   // 1=Sunday, 2=Monday, ..., 7=Saturday
  timeSlotIds: number[];
}
```

### Request Sample

```json
{
  "daySchedules": [
    { "dayOfWeekId": 1, "timeSlotIds": [1, 2, 3] },
    { "dayOfWeekId": 3, "timeSlotIds": [4, 5] }
  ]
}
```

### Response

Returns `TeacherAvailabilityResponseDto` containing the full weekly schedule + any exceptions.

### Related Endpoints

- `GET /Api/V1/Teacher/TeacherAvailability?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD` — get schedule + exceptions
- `POST /Api/V1/Teacher/TeacherAvailability/Exception` — add a one-off exception (vacation/extra slot)
- `DELETE /Api/V1/Teacher/TeacherAvailability/Exception/{id}` — remove an exception

After saving availability, registration is complete → navigate to dashboard.

---

## Lookup / Helper Endpoints

### Get Identity Types

**`GET /Api/V1/Authentication/IdentityTypes?isInSaudiArabia=true`**

Query param `isInSaudiArabia` filters the list:
- `true` → NationalId, Iqama
- `false` → Passport, DrivingLicense
- omitted → all four

Returns `EnumItemDto[]` with `id`, `nameAr`, `nameEn`.

### Get Document Types

**`GET /Api/V1/Authentication/DocumentTypes`**

Returns the teacher document type enum (IdentityDocument, Certificate, Other) with translations.

---

## Error Handling

### Validation Errors (400)

```json
{
  "statusCode": "BadRequest",
  "succeeded": false,
  "message": "Validation errors occurred.",
  "data": null,
  "errors": [
    "Password must contain at least one uppercase letter",
    "First name is required"
  ]
}
```

Display each error from the `errors` array under the relevant field or as a toast.

### Unauthorized (401)

Token expired, invalid, or missing required role. Most common cause for teachers: calling `/UploadDocuments` with the pre-`CompletePersonalInfo` token (which lacks the `Teacher` role). Redirect to phone/OTP screen.

### Account Blocked (400)

```json
{
  "succeeded": false,
  "message": "Your account has been blocked. Please contact support.",
  "data": null
}
```

Surface this prominently — do not allow retry on the OTP screen.

---

## Token Management

```typescript
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

**Token lifecycle:**
1. `LoginOrRegister` — no token returned (just sends OTP)
2. `VerifyOtp` — returns initial token (no `Teacher` role yet for new users)
3. `CompletePersonalInfo` — returns NEW token (includes `Teacher` role) — **MUST replace**
4. `UploadDocuments` — no token change
5. Re-login via OTP at any time — fresh token with current role/status

---

## Navigation State Machine

```typescript
interface TeacherAuthState {
  token: string | null;
  currentStep: number;
  nextStepName: string;
  isRegistrationComplete: boolean;
  rejectedDocuments: RejectedDocumentInfo[] | null;
}

function navigateByNextStep(nextStepName: string) {
  const routes: Record<string, string> = {
    'Complete Personal Information':   '/register/personal-info',
    'Upload Documents':                '/register/upload-documents',
    'Awaiting Admin Verification':     '/register/pending',
    'Re-upload Rejected Documents':    '/register/reupload',
    'Add Teaching Subjects and Units': '/register/subjects',
    'Set Your Availability':           '/register/availability',
    'Registration Complete':           '/dashboard',
  };
  navigate(routes[nextStepName] || '/dashboard');
}
```

The server drives the flow — always follow `nextStep.nextStepName` rather than hardcoding step logic on the frontend. On app cold-start, you can re-fetch the current status by calling `/VerifyOtp` again (or hit a profile endpoint).

---

## Enums Reference

### Teacher Status (server-side)

| Value | Name | Meaning |
|-------|------|---------|
| 1 | AwaitingDocuments | Personal info done, needs to upload documents |
| 2 | PendingVerification | Documents uploaded, awaiting admin review |
| 3 | DocumentsRejected | Some documents rejected — needs re-upload |
| 4 | Active | Approved — can teach |
| 5 | Blocked | Account blocked by admin |

### Identity Type (request value)

| Value | Name | Used When |
|-------|------|-----------|
| 1 | NationalId | Inside Saudi Arabia |
| 2 | Iqama | Inside Saudi Arabia |
| 3 | Passport | Outside Saudi Arabia (requires `IssuingCountryCode`) |
| 4 | DrivingLicense | Outside Saudi Arabia (requires `IssuingCountryCode`) |

### Teacher Document Type

| Value | Name |
|-------|------|
| 1 | IdentityDocument |
| 2 | Certificate |
| 3 | Other |

### Document Verification Status

| Value | Name |
|-------|------|
| 1 | Pending |
| 2 | Approved |
| 3 | Rejected |

### Day of Week (availability)

| Value | Day |
|-------|-----|
| 1 | Sunday |
| 2 | Monday |
| 3 | Tuesday |
| 4 | Wednesday |
| 5 | Thursday |
| 6 | Friday |
| 7 | Saturday |

### Quran Content Type (subject units, optional)

| Value | Name AR | Name EN |
|-------|---------|---------|
| 1 | حفظ | Memorization |
| 2 | تلاوة | Recitation |
| 3 | تجويد | Tajweed |

### Quran Level (subject units, optional)

| Value | Name AR | Name EN |
|-------|---------|---------|
| 1 | نوراني | Noorani |
| 2 | مبتدئ | Beginner |
| 3 | متوسط | Intermediate |
| 4 | متقدم | Advanced |
